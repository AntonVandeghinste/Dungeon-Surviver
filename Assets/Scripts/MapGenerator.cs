using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class MapGenerator : MonoBehaviour {

	public float roomSize = 1;
	public int defaultWidth = 2, defaultHeight = 2;
	public bool showAddedRooms = true;

	int fillPercent = 65;
	int width, height;
	string seed;
	float spacing;

	Room[,] map;
	List<Room> rooms = new List<Room>();
	List<Room> addedRooms = new List<Room> ();
	bool RoomListNeedsUpdating = false;

	void Update () {
		
		if (Input.GetMouseButtonDown (0))
			SetupScene (GameManager.instance.level);

	}

	public void SetupScene (int level) {

		rooms.Clear ();
		addedRooms.Clear ();
		flags = null;

		width = defaultWidth + (int)Mathf.Log (level, 2f);
		height = defaultHeight + (int)Mathf.Log (level, 2f);

		spacing = roomSize * 0.10f;

		roomSize += spacing;

		seed = System.DateTime.Now.ToString ();

		map = new Room[width, height];
		RandomFillMap ();

		MakeMapSolvable ();

	}

	void RandomFillMap () {

		System.Random randomGenerator = new System.Random (seed.GetHashCode ());

		for (int x = 0; x < width; x++) {

			for (int y = 0; y < height; y++) {

				map[x, y] = (randomGenerator.Next (0, 100) < fillPercent) ? new Room (x, y) : null;
				if (map[x, y] != null)
					rooms.Add (map[x, y]);

			}

		}

	}

	void MakeMapSolvable () {

		int mainRoom = UnityEngine.Random.Range (0, rooms.Count);
		rooms [mainRoom].isMainRoom = true;
		rooms [mainRoom].SetAccesibleFromMainRoom ();
		rooms [mainRoom].type = (int)Room.Rooms.Spawn;

		List<List<Room>> allRoomGroups = GetConnectedRooms ();
		for (int i = 0; i < (allRoomGroups.Count / 2) - 1; i++) {

			MakeRoomsAccesible ();
			if (RoomListNeedsUpdating)
				UpdateRooms ();
			flags = null;

		}
	

	}

	int[] flags = null;
	void MakeRoomsAccesible (bool forceAccessibilityFromMainRoom = false, int[] flgs = null) {

		List<List<Room>> allRoomGroups = GetConnectedRooms ();
		//allRoomGroups.ForEach(x => x.ForEach (UnityEngine.Debug.Log));
		//UnityEngine.Debug.Log (allRoomGroups.Count);
		if (flags == null)
			flags = new int[allRoomGroups.Count];
		if (flgs != null)
			flags = flgs;

		int bestDistance = 0;
		Room bestRoomA = new Room();
		Room bestRoomB = new Room();
		bool possibleConnectionFound = false;

		if (allRoomGroups.Count > 1) {

			foreach (List<Room> roomGroupA in allRoomGroups) {

				if (!forceAccessibilityFromMainRoom) {
					
					possibleConnectionFound = false;
					
					if (flags[allRoomGroups.IndexOf(roomGroupA)] == 1)
						continue;
					
				}

				foreach (List<Room> roomGroupB in allRoomGroups) {

					if (roomGroupA == roomGroupB || (flags[allRoomGroups.IndexOf(roomGroupA)] == 1 && flags[allRoomGroups.IndexOf(roomGroupB)] == 1))
						continue;

					for (int a = 0; a < roomGroupA.Count; a++) {

						for (int b = 0; b < roomGroupB.Count; b++) {

							Room roomA = roomGroupA[a];
							Room roomB = roomGroupB[b];

							int distanceBetweenRooms = (int) (Mathf.Pow (roomA.position.X - roomB.position.X, 2) + Mathf.Pow (roomA.position.Y - roomB.position.Y, 2));

							if (distanceBetweenRooms < bestDistance || !possibleConnectionFound) {

								bestDistance = distanceBetweenRooms;
								possibleConnectionFound = true;
								bestRoomA = roomA;
								bestRoomB = roomB;

							}

						}

					}

				}
			
				if (possibleConnectionFound && !forceAccessibilityFromMainRoom) {

					AddRoomsToMakeAccessible (bestRoomA, bestRoomB);
					flags[allRoomGroups.IndexOf(roomGroupA)] = 1;
					flags[allRoomGroups.IndexOf (allRoomGroups.Find (item => item.Contains (bestRoomB)))] = 1;

				}
				
			}

			if (possibleConnectionFound && forceAccessibilityFromMainRoom) {

				AddRoomsToMakeAccessible (bestRoomA, bestRoomB);
				flags[allRoomGroups.IndexOf (allRoomGroups.Find (item => item.Contains (bestRoomA)))] = 1;
				flags[allRoomGroups.IndexOf (allRoomGroups.Find (item => item.Contains (bestRoomB)))] = 1;
				MakeRoomsAccesible(true, flags);
				
			}

			if (!forceAccessibilityFromMainRoom) {


				MakeRoomsAccesible(true, flags);
			}

		}

	}

	void UpdateRooms () {

		foreach (Room r in addedRooms) {

			rooms.Add (r);

		}

		addedRooms.Clear ();

	}

	void AddRoomsToMakeAccessible (Room a, Room b) {

		//UnityEngine.Debug.DrawLine (CoordToWorldPoint (a.position), CoordToWorldPoint (b.position), Color.red, 30);

		List<Coord> line = GetLine (a.position, b.position);

		foreach (Coord c in line) {

			map [c.X, c.Y] = new Room (c.X, c.Y);
			map [c.X, c.Y].type = (int)Room.Rooms.Test;
			addedRooms.Add (map [c.X, c.Y]);

		}

		RoomListNeedsUpdating = true;
		
		//addedRooms.ForEach (UnityEngine.Debug.Log);

	}

	Vector3 CoordToWorldPoint (Coord point) {
			
			return new Vector3 (((-width / 2f + point.X) * roomSize) + roomSize / 2f, 0, ((-height / 2f + point.Y) * roomSize) + roomSize / 2f);
			
	}

	List<Coord> GetLine (Coord from, Coord to) {
		
		List<Coord> line = new List<Coord> ();
		
		bool inverted = false;
		
		int x = from.X;
		int y = from.Y;
		
		int dx = to.X - x;
		int dy = to.Y - y;

		int ddx = 2 * dx;
		int ddy = 2 * dy;
		
		int step = Math.Sign (dx);
		int gradientStep = Math.Sign (dy);
		
		int longest = Mathf.Abs (dx);
		int shortest = Mathf.Abs (dy);
		
		if (longest < shortest) {
			
			inverted = true;
			longest = Mathf.Abs (dy);
			shortest = Mathf.Abs (dx);
			
			step = Math.Sign (dy);
			gradientStep = Math.Sign (dx);
			
		}
		
		int gA = longest / 2;
		int errorPrev = gA;
		for (int i = 0; i < longest; i++) {

			if (i > 0) {

				Coord t = new Coord();
				t.X = x;

				t.Y = y;
				line.Add (t);

			}
			
			if (inverted) {
				
				y += step;
				
			} else {
				
				x += step;
				
			}
			
			gA += shortest;
			
			if (gA >= longest) {
				
				if (inverted)
					x += gradientStep;
				else
					y += gradientStep;
				
				gA -= longest;

				if (gA + errorPrev < longest) {
					
					Coord t = new Coord();
					t.X = inverted ? x - gradientStep : x - step;
					
					t.Y = y;
					line.Add (t);
					
				} else if (gA + errorPrev > longest) {
					
					Coord t = new Coord();
					t.X = x;
					
					t.Y = inverted ? y - step : y - gradientStep;
					line.Add (t);
					
				} else {
					
					if (UnityEngine.Random.Range (0, 100) > 50) {
						Coord t = new Coord();
						t.X = x;
						
						t.Y = y - gradientStep;
						line.Add (t);
					} else {

						Coord t = new Coord();
						t.X = x - step;
						
						t.Y = y;
						line.Add (t);

					}
					
				}
				
			}
			
		}
		
		return line;
		
	}

	List<List<Room>> GetConnectedRooms () {

		List<List<Room>> connectedRooms = new List<List<Room>> ();
		int[,] mapFlags = new int[width, height];

		for (int x = 0; x < width; x++) {

			for (int y = 0; y < height; y++) {

				if (mapFlags[x, y] == 0 && rooms.Contains (map[x, y])) {

					List<Room> linkedRooms = GetRoomsInConnection(x, y);
					connectedRooms.Add (linkedRooms);

					foreach (Room r in linkedRooms) {

						mapFlags[r.position.X, r.position.Y] = 1;

					}

				}

			}

		}

		return connectedRooms;

	}

	List<Room> GetRoomsInConnection(int x, int y) {

		List<Room> connectedRooms = new List<Room> ();
		int[,] mapFlags = new int[width, height];

		Queue<Room> queue = new Queue<Room> ();
		queue.Enqueue (map [x, y]);
		mapFlags [x, y] = 1;

		while (queue.Count > 0) {

			Room room = queue.Dequeue ();
			connectedRooms.Add (room);

			for (int nX = room.position.X - 1; nX <= room.position.X + 1; nX++) {

				for (int nY = room.position.Y - 1; nY <= room.position.Y + 1; nY++) {

					if (IsInMapRange (nX, nY) && (nX == room.position.X || nY == room.position.Y)) {

						if (mapFlags[nX, nY] == 0 && rooms.Contains (map[nX, nY])) {

							mapFlags[nX, nY] = 1;
							queue.Enqueue (map[nX, nY]);

						}

					}

				}

			}

		}

		return connectedRooms;

	}

	bool IsInMapRange (int x, int y) {

		return x >= 0 && x < width && y >= 0 && y < height;

	}

	void OnDrawGizmos () {

		if (map != null) {

			for (int x = 0; x < width; x++) {
				
				for (int y = 0; y < height; y++) {

					if (map [x, y] != null) {

						if (map[x, y].type == (int) Room.Rooms.Spawn)
							Gizmos.color = Color.red;
						else
							Gizmos.color = Color.white;
						if (!showAddedRooms && map[x, y].type == (int) Room.Rooms.Test)
							continue;
						Vector3 pos = new Vector3 (((-width / 2f + x) * roomSize) + roomSize / 2f, 0, ((-height / 2f + y) * roomSize) + roomSize / 2f);
						Gizmos.DrawCube (pos, new Vector3 (roomSize - spacing, roomSize - spacing, roomSize - spacing));

					}

				}

			}

		}

	}

	[DebuggerDisplay("({x}, {y})")]
	struct Coord {
		
		private int x;
		private int y;
		
		public int X {

			get{

				return x;

			}
			set{

				x = value;

			}

		}

		public int Y {
			
			get{
				
				return y;
				
			}
			set{
				
				y = value;
				
			}
			
		}
		
	}

	[DebuggerDisplay("{Name} at position: {position}")]
	class Room {
		
		public Coord position;
		public List<Room> connectedRooms;
		public bool isAccessibleFromMainRoom;
		public bool isMainRoom;
		public int type;
		public enum Rooms {Spawn = 1, Test = 2};
		
		public Room() {
		}
		
		public Room (int x, int y) {

			connectedRooms = new List<Room> ();

			position = new Coord();
			position.X = x;
			position.Y = y;

		}
		
		public void SetAccesibleFromMainRoom () {
			
			if (!isAccessibleFromMainRoom) {
				
				isAccessibleFromMainRoom = true;
				foreach (Room connectedRoom in connectedRooms) {
					
					connectedRoom.SetAccesibleFromMainRoom();
					
				}
				
			}
			
		}
		
		public static void ConnectRooms (Room roomA, Room roomB) {
			
			if (roomA.isAccessibleFromMainRoom) {
				
				roomB.SetAccesibleFromMainRoom ();
				
			} else if (roomB.isAccessibleFromMainRoom) {
				
				roomA.SetAccesibleFromMainRoom ();
				
			}
			
			roomA.connectedRooms.Add (roomB);
			roomB.connectedRooms.Add (roomA);
			
		}
		
		public bool IsConnectedTo (Room otherRoom) {
			
			return connectedRooms.Contains (otherRoom);
			
		}

		public override string ToString () {

			return "Room at position: (" + position.X + ", " + position.Y + ")";

		}
		
	}

}