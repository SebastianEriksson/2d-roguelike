using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Completed;

//
// example usage of this class:
//
// MapGenerator map = ...;
// int width = map.MapWidth;
// int height = map.MapHeight;
//
// bool ok = map.GetContentAt(x, y).CanMoveTo();
// bool ok = map.GetContentAt(x, y).HasPlayer();
// map.SetContentAt(TileContent.Zombie, x, y);    // set that a zombie is in this tile
// map.RemoveContentAt(TileContent.Player, x, y); // set that the player is no longer in this tile
//

public class MapGenerator : MonoBehaviour {

	// roomWidth = sizeMultiplier * level + sizeConstant
	public float roomWidthConstant = 9;
	public float roomWidthMultiplier = 1f/9f;

	// turns out, this class can be useful
	[Serializable]
	public class Count {

		public float minCoefficient;
		public float maxCoefficient;

		public Count(float minCoefficient, float maxCoefficient) {
			this.minCoefficient = minCoefficient;
			this.maxCoefficient = maxCoefficient;
		}

		public int PickRandom(int width) {
			int min = Mathf.RoundToInt(minCoefficient * width * width);
			int max = Mathf.RoundToInt(maxCoefficient * width * width);
			return Random.Range(min, max + 1);
		}

	}

	public Count foodCount = new Count(1f/36f, 5/36f);
	public Count wallCount = new Count(5f/36f, 9/36f);

	public int corridorLength = 1;

	[Serializable]
	public class VaryingSpriteTile {
		
		public GameObject prefab;
		public Sprite[] sprites;

		public GameObject InstantiateRandom(int x, int y) {
			GameObject result = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity);
			int spriteIndex = Random.Range(0, sprites.Length);
			result.GetComponent<SpriteRenderer>().sprite = sprites[spriteIndex];
			return result;
		}

	}

	public VaryingSpriteTile outerWallTiles;
	public VaryingSpriteTile groundTiles;
	public VaryingSpriteTile wallTiles;
	public GameObject exitPrefab;

	// prefabs for now, because besides sprites, the tags also differ
	public GameObject[] foodPrefabs;

	public GameObject[] enemyPrefabs;

	public int MapWidth { get { return mapWidth; } }
	public int MapHeight { get { return mapHeight; } }

	// returns the contents of the specified tile
	// use the functions defined in TileContent.cs to query it
	public TileContent GetContentAt(Point point) {
		return map[point.x, point.y];
	}

	// adds the specified item to them, if it's not already there
	public void SetContentAt(TileContent whatToSet, Point point) {
		map[point.x, point.y] = map[point.x, point.y] | whatToSet;
	}

	// removes the specified item from the map, if it's there in the first place
	public void RemoveContentAt(TileContent whatToRemove, Point point) {
		map[point.x, point.y] = map[point.x, point.y] & (~whatToRemove);
	}

	// returns whether or not there is a tile at this point
	public bool PointIsValid(Point point) {
		return point.x >= 0 && point.y >= 0 && point.x < mapWidth && point.y < mapHeight;
	}

	private int level;

	// rooms are square, each is the same size in a single level
	private int roomWidth;

	// the origin point (lower bottom corner) of each room
	Point[] roomOrigins;

	private int mapWidth;
	private int mapHeight;
	private TileContent[,] map;

	public void MakeNewMap(int level) {

		this.level = level;

		Analytics.ReportLevelStart(level, level == 1);

		InitializeMap(CreateRoomOrigins());

		PlaceRooms();

		MakeGameObjects();

		MovePlayerToTheFirstRoom();

	}

	// returns the minimum x value of all the origin points created
	private int CreateRoomOrigins() {

		Point lastOrigin = new Point();
		roomOrigins = new Point[level];
		roomOrigins[0] = lastOrigin;

		int minX = 0;

		int lastDir = 0; // -1 is left, 0 is up, 1 is right

		roomWidth = Mathf.RoundToInt(roomWidthMultiplier * level + roomWidthConstant);
		int nextRoomDelta = roomWidth + corridorLength;

		for (int i = 1; i < level; i += 1) {

			float random = Random.value;

			if (lastDir == 0) {
				if (random < 0.4f) { lastDir = -1; }
				else if (random > 0.6f) { lastDir = 1; }
				else { lastDir = 0; }
			} else if (random < 0.8f) {
				lastDir = 0;
			}

			if (lastDir == -1) {
				lastOrigin.x -= nextRoomDelta;
				if (lastOrigin.x < minX) { minX = lastOrigin.x; }
			} else if (lastDir == 0) {
				lastOrigin.y += nextRoomDelta;
			} else /* (lastDir == 1) */ {
				lastOrigin.x += nextRoomDelta;
			}

			roomOrigins[i] = lastOrigin;

		}

		return minX;

	}

	private void InitializeMap(int minX) {
		
		int maxX = 0;
		int maxY = 0;

		for (int i = 0; i < level; i += 1) {
			
			Point origin = roomOrigins[i];
			origin.x -= minX;
			roomOrigins[i] = origin;

			if (origin.x > maxX) { maxX = origin.x; }
			if (origin.y > maxY) { maxY = origin.y; }

		}

		mapWidth = maxX + roomWidth;
		mapHeight = maxY + roomWidth;

		map = new TileContent[mapWidth, mapHeight];

	}

	private void PlaceRooms() {

		PlaceRoom(roomOrigins[0]);

		int finalRoom = level - 1;

		for (int i = 1; i <= finalRoom; i += 1) {

			PlaceRoom(roomOrigins[i]);
			ConnectRooms(roomOrigins[i - 1], roomOrigins[i], i == finalRoom);

		}

		if (level == 1) {
			PlaceExit(roomOrigins[0], new Point(1, 1));
		}

	}

	private void PlaceRoom(Point origin) {

		int maxX = origin.x + roomWidth - 1;
		int maxY = origin.y + roomWidth - 1;

		// horizontal outer walls
		for (int x = origin.x; x <= maxX; x += 1) {
			map[x, origin.y] = TileContent.OuterWall;
			map[x, maxY] = TileContent.OuterWall;
		}

		// vertical outer walls
		for (int y = origin.y + 1; y < maxY; y += 1) {
			map[origin.x, y] = TileContent.OuterWall;
			map[maxX, y] = TileContent.OuterWall;
		}

		// ground
		for (int x = origin.x + 1; x < maxX; x += 1) {
			for (int y = origin.y + 1; y < maxY; y += 1) {
				map[x, y] = TileContent.Ground;
			}
		}

		PutStuffInRoom(origin);

	}

	private int currentRoomRemainingFreeSpaces;
	private Point currentRoomOrigin;

	private void PutStuffInRoom(Point origin) {

		currentRoomRemainingFreeSpaces = roomWidth - 4;
		currentRoomRemainingFreeSpaces *= currentRoomRemainingFreeSpaces;
		currentRoomOrigin = origin;

		PlaceStuff(foodCount.PickRandom(roomWidth - 4), TileContent.Food);
		PlaceStuff(wallCount.PickRandom(roomWidth - 4), TileContent.Wall);
		PlaceStuff(level - 1, TileContent.Zombie);

	}

	private Point PickRandomPointForStuffInCurrentRoom() {
		
		int offset = Random.Range(0, currentRoomRemainingFreeSpaces);
		currentRoomRemainingFreeSpaces -= 1;

		int minX = currentRoomOrigin.x + 2;
		int endX = currentRoomOrigin.x + roomWidth - 2;
		int minY = currentRoomOrigin.y + 2;
		int endY = currentRoomOrigin.y + roomWidth - 2;

		for (int x = minX; x < endX; x += 1) {
			for (int y = minY; y < endY; y += 1) {
				if (map[x, y] != TileContent.Ground) { continue; }
				if (offset == 0) { return new Point(x, y); }
				offset -= 1;
			}
		}

		// should never reach this point
		return new Point(-1, -1);

	}

	private void PlaceStuff(int count, TileContent type) {
		
		for (int i = 0; i < count; i += 1) {
			Point location = PickRandomPointForStuffInCurrentRoom();
			map[location.x, location.y] = map[location.x, location.y] | type;
		}

	}

	private void ConnectRooms(Point from, Point to, bool placeExit) {

		int offset = Random.Range(1, roomWidth - 1);

		if (from.x == to.x) { // up

			int x = from.x + offset;

			for (int y = from.y + roomWidth - 1; y <= to.y; y += 1) {
				map[x - 1, y] = TileContent.OuterWall;
				map[x, y] = TileContent.Ground;
				map[x + 1, y] = TileContent.OuterWall;
			}

			if (placeExit) { PlaceExit(to, new Point(offset < roomWidth / 2 ? 1 : 0, 1)); }

		} else { // left or right

			int y = from.y + offset;
			int minX;
			int maxX;
			int horizontalExit;

			if (from.x < to.x) {
				minX = from.x + roomWidth - 1;
				maxX = to.x;
				horizontalExit = 1;
			} else {
				minX = to.x + roomWidth - 1;
				maxX = from.x;
				horizontalExit = 0;
			}

			for (int x = minX; x <= maxX; x += 1) {
				map[x, y - 1] = TileContent.OuterWall;
				map[x, y] = TileContent.Ground;
				map[x, y + 1] = TileContent.OuterWall;
			}

			if (placeExit) { PlaceExit(to, new Point(horizontalExit, offset < roomWidth / 2 ? 1 : 0)); }

		}

	}

	// (0, 0) - lower left corner
	// (1, 0) - lower right corner
	// (0, 1) - upper left corner
	// (1, 1) - upper right corner
	private void PlaceExit(Point roomOrigin, Point corner) {
		int x = roomOrigin.x + 1 + (roomWidth - 3) * corner.x;
		int y = roomOrigin.y + 1 + (roomWidth - 3) * corner.y;
		map[x, y] = map[x, y] | TileContent.Exit;
	}

	private void MakeGameObjects() {

		for (int x = 0; x < mapWidth; x += 1) {
			for (int y = 0; y < mapHeight; y += 1) {
				MakeGameObjectsAt(x, y);
			}
		}

	}

	private void MakeGameObjectsAt(int x, int y) {

		TileContent contents = map[x, y];

		if (contents == 0) { return; }

		if (contents == TileContent.OuterWall) {
			outerWallTiles.InstantiateRandom(x, y);
			return;
		}

		Debug.Assert((contents & TileContent.OuterWall) == 0, "an outer wall shouldn't contain other stuff");
		Debug.Assert((contents & TileContent.Ground) != 0, "stuff should be on the ground");
		groundTiles.InstantiateRandom(x, y);

		if ((contents & TileContent.Exit) != 0) {
			Instantiate(exitPrefab, new Vector3(x, y, 0), Quaternion.identity);
		}

		if ((contents & TileContent.Food) != 0) {
			int prefabIndex = Random.Range(0, foodPrefabs.Length);
			Instantiate(foodPrefabs[prefabIndex], new Vector3(x, y, 0), Quaternion.identity);
		}

		if ((contents & TileContent.Wall) != 0) {
			GameObject wall = wallTiles.InstantiateRandom(x, y);
			wall.GetComponent<Wall>().modelPosition = new Point(x, y);
		}

		if ((contents & TileContent.Zombie) != 0) {
			int prefabIndex = Random.Range(0, enemyPrefabs.Length);
			GameObject enemy = Instantiate(enemyPrefabs[prefabIndex], new Vector3(x, y, 0), Quaternion.identity);
			enemy.GetComponent<MovingObject>().modelPosition = new Point(x, y);
		}

	}

	private void MovePlayerToTheFirstRoom() {
		Point origin = roomOrigins[0];
		int x = origin.x + 1;
		int y = origin.y + 1;
		GameObject player = GameObject.Find("Player");
		player.transform.position = new Vector3(x, y, 0);
		map[x, y] = map[x, y] | TileContent.Player;
		player.GetComponent<MovingObject>().modelPosition = new Point(x, y);
	}

}
