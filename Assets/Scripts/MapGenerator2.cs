using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MapGenerator2 : MonoBehaviour
{

	public int roomSize = 1;
	public int defaultWidth = 2, defaultHeight = 2, defaultDepth = 2;

	int fillPercent = 65;
	static int width, height, depth;//respectively x,z,y
	string seed;
	float spacing = 1.10f;
	List<Room> addedRooms = new List<Room> ();
	bool updateRoomlist;
	Room exit;

	Room[,,] map;
	//This is for simple coordinate wise accessibilty(coordinate 0,0,0 namely the first block will have a real coordinate of -somewhat,y,z etc...)

	List<Room> rooms = new List<Room>();
	//This is the actual list of rooms

	void Update ()
	{

		if (Input.GetMouseButtonDown (0))
			SetupScene (GameManager.instance.level);//debug only

	}

	public void SetupScene (int level)
	{

		rooms.Clear ();//make sure we start with nothing

		width = defaultWidth + (int)Mathf.Log (level, 2f);
		height = defaultHeight + (int)Mathf.Log (level, 2f);
		depth = defaultDepth + (int)Mathf.Log (level, 2f);

		Room.setRoomSize (roomSize);
		Room.setDrawSpace (roomSize * spacing);

		seed = System.DateTime.Now.ToString ();

		map = new Room[width, depth, height];

		//we are initialized so generate the map:
		int i = 0;
		while (!RandomFillMap ()) {

			if(i >= 5)
				throw new ApplicationException("Could not find a map after 5 tries");
			i++;

		}


		//set a spawnroom:
		int spawn = UnityEngine.Random.Range (0, rooms.Count);
		rooms [spawn].setType (Type.SPAWN);
		rooms [spawn].SetAccesibleFromSpawn ();

		//we have a filled map and list with Room objects, now we make it so that there's no 'unreachable' rooms:
		MakeRoomsAccesible ();

		//Now that our rooms are all accessible from any other room(be it through other rooms or not)
		//we can start to generate a simple maze-like connection between the rooms using recursive backtracking in PathGenerator
		//The odd thing here is we do not define an exit yet, we will define the exit later with the requirement to be a dead end.
		//Also we'll add some keys and locks if the exit room is for instance right next to the spawn room, but what are the chances right? ...right...
		PathGenerator.GeneratePath (map, rooms);

		//We have a maze with a path, now we choose an exit room
		exit = AssignExitRoom ();

		//Exit room exists, almost done with the calculations, let's make it somewhat harder for the user to reach the exit room
		CalculateRequiredKeys ();

	}

	bool RandomFillMap ()
	{

		System.Random randomHash = new System.Random (seed.GetHashCode ());

		for (int x = 0; x < width; x++) {

			for (int y = 0; y < depth; y++) {

				for (int z = 0; z < height; z++) {

					Vector3 pos = new Vector3(x, y, z);
					map[x, y, z] = (randomHash.Next (0, 100) < fillPercent) ? new Room (pos) : null;

					if (map[x, y, z] != null) {

						rooms.Add (map[x, y, z]);

					}

				}

			}

		}

		if (rooms.Count < 2) {
			rooms.Clear ();
			return false;
		}
		return true;

	}

	void MakeRoomsAccesible (bool forceAcces = false)
	{

		if (updateRoomlist) {

			UpdateRoomList ();
			addedRooms.Clear ();
			updateRoomlist = false;

		}

		List<Room> roomsListA = new List<Room> ();//not accessible from spawn
		List<Room> roomsListB = new List<Room> ();//accessible from spawn

		if (forceAcces) {

			foreach (Room room in rooms) {

				if (room.IsAccessibleFromSpawn ()) {

					roomsListB.Add (room);

				} else {

					roomsListA.Add (room);

				}

			}

		} else {

			roomsListA = rooms;
			roomsListB = rooms;

		}

		int bestDistance = 0;
		Room bestRoomA = new Room ();
		Room bestRoomB = new Room ();
		bool possibleConnectionFound = false;

		foreach(Room roomA in roomsListA) {

			if(!forceAcces){

				possibleConnectionFound = false;

				if(roomA.AccessibleRooms().Count > 0)
					continue;

			}

			foreach(Room roomB in roomsListB) {

				if(roomA == roomB || roomA.HasAccessTo(roomB))
					continue;

				int distanceBetweenRooms = (int) (Mathf.Pow (roomA.getPosition().x - roomB.getPosition().x, 2) + Mathf.Pow (roomA.getPosition().y - roomB.getPosition().y, 2) + Mathf.Pow (roomA.getPosition().z -roomB.getPosition().z, 2));

				if(distanceBetweenRooms < bestDistance || !possibleConnectionFound) {

					bestDistance = distanceBetweenRooms;
					possibleConnectionFound = true;
					bestRoomA = roomA;
					bestRoomB = roomB;
				}

			}

			if(possibleConnectionFound && !forceAcces) {

				AddRoomsToMakeAccessible (bestRoomA, bestRoomB);

			}

		}

		if(possibleConnectionFound && forceAcces) {

			AddRoomsToMakeAccessible (bestRoomA, bestRoomB);
			MakeRoomsAccesible(true);

		}

		if(!forceAcces) {

			MakeRoomsAccesible (true);

		}

	}

	void UpdateRoomList() {

		foreach (Room room in addedRooms) {

			rooms.Add (room);

		}

	}

	void AddRoomsToMakeAccessible(Room a, Room b) {

		//Debug.DrawLine (new Vector3 (((-width / 2f + a.getPosition ().x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + a.getPosition ().y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + a.getPosition ().z) * roomSize * spacing) + roomSize * spacing / 2f), new Vector3 (((-width / 2f + b.getPosition ().x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + b.getPosition ().y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + b.getPosition ().z) * roomSize * spacing) + roomSize * spacing / 2f), Color.green, 120);

		List<Vector3> line = GetLineByVoxelTraversal (a.getPosition (), b.getPosition ());

		Room lastRoom = a;
		foreach (Vector3 point in line) {

			map[(int)point.x, (int)point.y, (int)point.z] = new Room(point);
			//map[(int)point.x, (int)point.y, (int)point.z].setType(Dungeon.Type.DEBUG);
			Room.MakeAccessBetween (map[(int)point.x, (int)point.y, (int)point.z], lastRoom);
			addedRooms.Add (map[(int)point.x, (int)point.y, (int)point.z]);
			//Debug.Log (point.ToString ());
			lastRoom = map[(int)point.x, (int)point.y, (int)point.z];

		}

		Room.MakeAccessBetween (lastRoom, b);

		updateRoomlist = true;

	}

	List<Vector3> GetLineByVoxelTraversal(Vector3 from, Vector3 to) {

		List<Vector3> line = new List<Vector3> ();

		int x, y, z, dx, dy, dz, n, sx, sy, sz, exy, exz, ezy, ax, ay, az, bx, by, bz;

		x = (int)from.x;
		y = (int)from.y;
		z = (int)from.z;

		dx = (int)to.x - (int)x;
		dy = (int)to.y - (int)y;
		dz = (int)to.z - (int)z;

		sx = Math.Sign (dx);
		sy = Math.Sign (dy);
		sz = Math.Sign (dz);

		ax = Math.Abs (dx);
		ay = Math.Abs (dy);
		az = Math.Abs (dz);

		bx = 2 * ax;
		by = 2 * ay;
		bz = 2 * az;

		exy = ay - ax;
		exz = az - ax;
		ezy = ay - az;

		n = ax + ay + az;

		while (--n >= 0) {

			if(map[x, y, z] == null) {

				line.Add (new Vector3(x, y, z));

			}

			if(exy < 0) {
				if(exz < 0) {

					x += sx;
					exy += by;
					exz += bz;

				} else {

					z += sz;
					exz -= bx;
					ezy += by;

				}
			} else {
				if(ezy < 0) {

					z += sz;
					exz -= bx;
					ezy += by;

				} else {

					y += sy;
					exy -= bx;
					ezy -= bz;

				}
			}

		}

		return line;

	}

	Room AssignExitRoom() {

		List<Room> deadEnds = new List<Room> ();

		foreach (Room r in rooms) {

			if(r.ConnectedRooms().Count < 2  && !r.TypeOf (Type.SPAWN)){

				r.SetDeadEnd();
				deadEnds.Add (r);

			}

		}

		int exit = UnityEngine.Random.Range (0, deadEnds.Count);
		Room old = rooms [rooms.IndexOf (deadEnds [exit])];
		Room n = new ExitRoom (old.getPosition ());
		Room connection = old.ConnectedRooms ()[0];
		old.DisconnectRooms ();
		Room.ConnectRooms (n, connection);
		int index = rooms.IndexOf (old);
		rooms [index] = n;
		map [(int)old.getPosition ().x, (int)old.getPosition ().y, (int)old.getPosition ().z] = n;

		return n;

	}

	void CalculateRequiredKeys(){

		int requiredKeys = 0;
		foreach (Room r in rooms) {
			
			if(r.IsDeadEnd()){

				requiredKeys++;
				
			}
			
		}

		requiredKeys += UnityEngine.Random.Range (0, rooms.Count - (2 + requiredKeys));
		requiredKeys += (int) Math.Log (GameManager.instance.level);

		((ExitRoom)exit).SetRequiredKeys (requiredKeys);

	}

	public static bool IsInMapRange (int x, int y, int z) {
								
		return x >= 0 && x < width && y >= 0 && y < depth && z >= 0 && z < height;

	}													                                                                

	void OnDrawGizmos () {
		
		if (map != null) {
			
			for (int x = 0; x < width; x++) {
				
				for (int y = 0; y < depth; y++) {
					
					for (int z = 0; z < height; z++) {

						if (map [x, y, z] != null) {

							Vector3 pos = new Vector3 (((-width / 2f + x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + z) * roomSize * spacing) + roomSize * spacing / 2f);
							if(map[x, y, z].TypeOf(Type.SPAWN))
								Gizmos.color = Color.white;
							else if(map[x, y, z].TypeOf(Type.EXIT))
								Gizmos.color = Color.blue;
							else
								Gizmos.color = Color.gray;
							Gizmos.DrawCube (pos, new Vector3 (.1f, .1f, .1f));
							List<Room> connections = map[x, y, z].ConnectedRooms();
							foreach(Room r in connections){

								Vector3 pos2 = new Vector3 (((-width / 2f + r.getPosition ().x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + r.getPosition ().y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + r.getPosition ().z) * roomSize * spacing) + roomSize * spacing / 2f);
								Gizmos.DrawLine (pos, pos2);

							}

						}

					}
					
				}
				
			}
			
		}
		
	}
	
}