using UnityEngine;

public struct Point {
	
	public int x;
	public int y;

	public Point(int x, int y) { this.x = x; this.y = y; }

	public Point Up { get { return new Point(x, y + 1); } }
	public Point Left { get { return new Point(x - 1, y); } }
	public Point Right { get { return new Point(x + 1, y); } }
	public Point Down { get { return new Point(x, y - 1); } }

	public Vector3 Vector { get { return new Vector3(x, y); } }

}
