using System;

// Bitmask of the tile contents.
[Flags]
public enum TileContent {
	OuterWall = 0x01, // The one that can't be interacted with.
	Ground    = 0x02, // Other stuff can also be on this tile.
	Food      = 0x04,
	Wall      = 0x08, // The breakable one.
	Player    = 0x10,
	Zombie    = 0x20,
	Exit      = 0x40
}

// Functions in this class can be called directly on a TileContent value.
public static class TileContentExtensions {

	// (1) must have ground
	// (2) must not have wall, player, or zombies
	public static bool CanMoveTo(this TileContent tile) {
		if ((tile & TileContent.Ground) == 0) { return false; } // (1)
		return (tile & (TileContent.Wall | TileContent.Player | TileContent.Zombie)) == 0; // (2)
	}

	// is wall, player, or zombie
	public static bool CanInteractWith(this TileContent tile) {
		return (tile & (TileContent.Wall | TileContent.Player | TileContent.Zombie)) != 0;
	}

	public static bool HasFood(this TileContent tile) {
		return (tile & TileContent.Food) != 0;
	}

	public static bool HasWall(this TileContent tile) {
		return (tile & TileContent.Wall) != 0;
	}

	public static bool HasPlayer(this TileContent tile) {
		return (tile & TileContent.Player) != 0;
	}

	public static bool HasZombie(this TileContent tile) {
		return (tile & TileContent.Zombie) != 0;
	}

	public static bool HasExit(this TileContent tile) {
		return (tile & TileContent.Exit) != 0;
	}

}
