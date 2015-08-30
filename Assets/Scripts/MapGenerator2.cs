using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MapGenerator2 : MonoBehaviour
{

	int fillPercent = 65;
	string seed;
	List<Room> addedRooms = new List<Room> ();
	bool updateRoomlist;

	void Update ()
	{

		if (Input.GetMouseButtonDown (0))
			SetupScene (GameManager.instance.level);//debug only

	}

	public void SetupScene (int level)
	{

		DungeonHelper.rooms.Clear ();//make sure we start with nothing
		DungeonHelper.deadEnds.Clear ();

		DungeonHelper.width = DungeonHelper.defaultWidth + (int)Mathf.Log (level, 2.5f);
		DungeonHelper.height = DungeonHelper.defaultHeight + (int)Mathf.Log (level, 2.5f);
		DungeonHelper.depth = DungeonHelper.defaultDepth + (int)Mathf.Log (level, 2.5f);

		Room.setRoomSize (DungeonHelper.roomSize);
		Room.setDrawSpace (DungeonHelper.roomSize * DungeonHelper.spacing);

		seed = System.DateTime.Now.ToString ();

		DungeonHelper.map = new Room[DungeonHelper.width, DungeonHelper.depth, DungeonHelper.height];

		//we are initialized so generate the map:
		int i = 0;
		while (!RandomFillMap ()) {

			if(i >= 5)
				throw new ApplicationException("Could not find a map after 5 tries");
			i++;

		}


		//set a spawnroom:
		int spawn = UnityEngine.Random.Range (0, DungeonHelper.rooms.Count);
		DungeonHelper.rooms [spawn].setType (Type.SPAWN);
		DungeonHelper.rooms [spawn].SetAccesibleFromSpawn ();
		DungeonHelper.Spawn = DungeonHelper.rooms [spawn];

		//we have a filled map and list with Room objects, now we make it so that there's no 'unreachable' rooms:
		MakeRoomsAccesible ();

		//Now that our rooms are all accessible from any other room(be it through other rooms or not)
		//we can start to generate a simple maze-like connection between the rooms using recursive backtracking in PathGenerator
		//The odd thing here is we do not define an exit yet, we will define the exit later with the requirement to be a dead end.
		//Also we'll add some keys and locks if the exit room is for instance right next to the spawn room, but what are the chances right? ...right...
		PathGenerator.GeneratePath (DungeonHelper.map, DungeonHelper.rooms);

		//We have a maze with a path, now we choose an exit room
		DungeonHelper.AssignExitRoom ();

		//Exit room exists, almost done with the calculations, let's make it somewhat harder for the user to reach the exit room
		DungeonHelper.CalculateRequiredKeys ();

		//let's add the right monsters and puzzles to the rooms shall we?
		DungeonHelper.AddGamePlay ();

		Debug.Log (DungeonHelper.rooms.Count);
		DungeonHelper.rooms.ForEach (item => Debug.Log (item));
		Debug.Log ("Required keys: " + DungeonHelper.RequiredKeys);

	}

	bool RandomFillMap ()
	{

		System.Random randomHash = new System.Random (seed.GetHashCode ());

		for (int x = 0; x < DungeonHelper.width; x++) {

			for (int y = 0; y < DungeonHelper.depth; y++) {

				for (int z = 0; z < DungeonHelper.height; z++) {

					Vector3 pos = new Vector3(x, y, z);
					DungeonHelper.map[x, y, z] = (randomHash.Next (0, 100) < fillPercent) ? new Room (pos) : null;

					if (DungeonHelper.map[x, y, z] != null) {

						DungeonHelper.rooms.Add (DungeonHelper.map[x, y, z]);

					}

				}

			}

		}

		if (DungeonHelper.rooms.Count < 2) {
			DungeonHelper.rooms.Clear ();
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

			foreach (Room room in DungeonHelper.rooms) {

				if (room.IsAccessibleFromSpawn ()) {

					roomsListB.Add (room);

				} else {

					roomsListA.Add (room);

				}

			}

		} else {

			roomsListA = DungeonHelper.rooms;
			roomsListB = DungeonHelper.rooms;

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

			DungeonHelper.rooms.Add (room);

		}

	}

	void AddRoomsToMakeAccessible(Room a, Room b) {

		//Debug.DrawLine (new Vector3 (((-width / 2f + a.getPosition ().x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + a.getPosition ().y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + a.getPosition ().z) * roomSize * spacing) + roomSize * spacing / 2f), new Vector3 (((-width / 2f + b.getPosition ().x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + b.getPosition ().y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + b.getPosition ().z) * roomSize * spacing) + roomSize * spacing / 2f), Color.green, 120);

		List<Vector3> line = GetLineByVoxelTraversal (a.getPosition (), b.getPosition ());

		Room lastRoom = a;
		foreach (Vector3 point in line) {

			DungeonHelper.map[(int)point.x, (int)point.y, (int)point.z] = new Room(point);
			//map[(int)point.x, (int)point.y, (int)point.z].setType(Dungeon.Type.DEBUG);
			Room.MakeAccessBetween (DungeonHelper.map[(int)point.x, (int)point.y, (int)point.z], lastRoom);
			addedRooms.Add (DungeonHelper.map[(int)point.x, (int)point.y, (int)point.z]);
			//Debug.Log (point.ToString ());
			lastRoom = DungeonHelper.map[(int)point.x, (int)point.y, (int)point.z];

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

			if(DungeonHelper.map[x, y, z] == null) {

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

	void OnDrawGizmos () {

		if (DungeonHelper.map != null) {

			for (int x = 0; x < DungeonHelper.width; x++) {
				
				for (int y = 0; y < DungeonHelper.depth; y++) {
					
					for (int z = 0; z < DungeonHelper.height; z++) {

						if (DungeonHelper.map [x, y, z] != null) {

							Vector3 pos = new Vector3 (((-DungeonHelper.width / 2f + x) * DungeonHelper.roomSize * DungeonHelper.spacing) + DungeonHelper.roomSize * DungeonHelper.spacing / 2f, ((-DungeonHelper.depth / 2f + y) * DungeonHelper.roomSize * DungeonHelper.spacing) + DungeonHelper.roomSize * DungeonHelper.spacing / 2f, ((-DungeonHelper.height / 2f + z) * DungeonHelper.roomSize * DungeonHelper.spacing) + DungeonHelper.roomSize * DungeonHelper.spacing / 2f);
							if(DungeonHelper.map[x, y, z] == DungeonHelper.Spawn)
								Gizmos.color = Color.white;
							else if(DungeonHelper.map[x, y, z] == DungeonHelper.Exit)
								Gizmos.color = Color.blue;
							else
								Gizmos.color = Color.gray;
							Gizmos.DrawCube (pos, new Vector3 (.1f, .1f, .1f));
							Gizmos.color = Color.gray;
							List<Room> connections = DungeonHelper.map[x, y, z].ConnectedRooms();
							foreach(Room r in connections){

								Vector3 pos2 = new Vector3 (((-DungeonHelper.width / 2f + r.getPosition ().x) * DungeonHelper.roomSize * DungeonHelper.spacing) + DungeonHelper.roomSize * DungeonHelper.spacing / 2f, ((-DungeonHelper.depth / 2f + r.getPosition ().y) * DungeonHelper.roomSize * DungeonHelper.spacing) + DungeonHelper.roomSize * DungeonHelper.spacing / 2f, ((-DungeonHelper.height / 2f + r.getPosition ().z) * DungeonHelper.roomSize * DungeonHelper.spacing) + DungeonHelper.roomSize * DungeonHelper.spacing / 2f);
								Gizmos.DrawLine (pos, pos2);

							}

						}

					}
					
				}
				
			}
			
		}
		
	}
	
}