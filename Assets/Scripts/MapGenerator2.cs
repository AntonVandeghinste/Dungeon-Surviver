using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Dungeon;

public class MapGenerator2 : MonoBehaviour
{

	enum Flags {

		Default,
		Processed,
		Done

	};

	public int roomSize = 1;
	public int defaultWidth = 2, defaultHeight = 2, defaultDepth = 2;

	int fillPercent = 65;
	int width, height, depth;//respectively x,z,y
	string seed;
	float spacing = 1.10f;
	Flags[] flags = null;
	List<Room> addedRooms = new List<Room> ();
	bool updateRoomlist;

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
		flags = null;

		width = defaultWidth + (int)Mathf.Log (level, 2f);
		height = defaultHeight + (int)Mathf.Log (level, 2f);
		depth = defaultDepth + (int)Mathf.Log (level, 2f);

		Room.setRoomSize (roomSize);
		Room.setDrawSpace (roomSize * spacing);

		seed = System.DateTime.Now.ToString ();

		map = new Room[width, depth, height];

		//we are initialized so generate the map:
		RandomFillMap ();

		//set a spawnroom:
		int spawn = UnityEngine.Random.Range (0, rooms.Count);
		rooms [spawn].setType (Dungeon.Type.SPAWN);
		rooms [spawn].SetAccesibleFromSpawn ();

		//we have a filled map and list with Room objects, now we make it so that there's no 'unreachable' rooms:
		MakeRoomsAccesible ();


	}

	void RandomFillMap ()
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

	}

	void MakeRoomsAccesible (bool forceAcces = false, Flags[] flags = null)
	{

		if (updateRoomlist) {

			UpdateRoomList ();
			addedRooms.Clear ();
			updateRoomlist = false;

		}

		List<List<Room>> interConnectedRooms = GetInterConnectedRooms ();
		if (this.flags == null)
			this.flags = new Flags[interConnectedRooms.Count];
		if (flags != null)
			this.flags = flags;

		int bestDistance = 0;
		Room bestRoomA = new Room ();
		Room bestRoomB = new Room ();
		bool possibleConnectionFound = false;

		if (interConnectedRooms.Count > 1) {

			foreach(List<Room> roomsA in interConnectedRooms) {

				if(!forceAcces){

					possibleConnectionFound = false;

					if(this.flags[interConnectedRooms.IndexOf (roomsA)] == Flags.Done)
						continue;

				}

				foreach(List<Room> roomsB in interConnectedRooms) {

					if(roomsA == roomsB || (this.flags[interConnectedRooms.IndexOf(roomsA)] == Flags.Done && this.flags[interConnectedRooms.IndexOf(roomsB)] == Flags.Done))
						continue;

					for(int a = 0; a < roomsA.Count; a++) {

						for(int b = 0; b < roomsB.Count; b++) {

							Room roomA = roomsA[a];
							Room roomB = roomsB[b];

							int distanceBetweenRooms = (int) (Mathf.Pow (roomA.getPosition().x - roomB.getPosition().x, 2) + Mathf.Pow (roomA.getPosition().y - roomB.getPosition().y, 2) + Mathf.Pow (roomA.getPosition().z -roomB.getPosition().z, 2));

							if(distanceBetweenRooms < bestDistance || !possibleConnectionFound) {

								bestDistance = distanceBetweenRooms;
								possibleConnectionFound = true;
								bestRoomA = roomA;
								bestRoomB = roomB;

							}

						}

					}

				}

				if(possibleConnectionFound && !forceAcces) {

					AddRoomsToMakeAccessible (bestRoomA, bestRoomB);
					this.flags[interConnectedRooms.IndexOf (roomsA)] = Flags.Processed;
					this.flags[interConnectedRooms.IndexOf (interConnectedRooms.Find (item => item.Contains (bestRoomB)))] = Flags.Processed;

				}

			}

			if(possibleConnectionFound && forceAcces) {

				AddRoomsToMakeAccessible (bestRoomA, bestRoomB);
				this.flags[interConnectedRooms.IndexOf (interConnectedRooms.Find (item => item.Contains (bestRoomA)))] = Flags.Done;
				this.flags[interConnectedRooms.IndexOf (interConnectedRooms.Find (item => item.Contains (bestRoomB)))] = Flags.Done;
				MakeRoomsAccesible(true, this.flags);

			}

			if(!forceAcces) {

				MakeRoomsAccesible (true, this.flags);

			}

		}

	}

	void UpdateRoomList() {

		foreach (Room room in addedRooms) {

			rooms.Add (room);

		}

	}

	void AddRoomsToMakeAccessible(Room a, Room b) {

		Debug.DrawLine (new Vector3 (((-width / 2f + a.getPosition ().x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + a.getPosition ().y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + a.getPosition ().z) * roomSize * spacing) + roomSize * spacing / 2f), new Vector3 (((-width / 2f + b.getPosition ().x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + b.getPosition ().y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + b.getPosition ().z) * roomSize * spacing) + roomSize * spacing / 2f), Color.green, 30);

		List<Vector3> line = GetLineByVoxelTraversal (a.getPosition (), b.getPosition ());

		foreach (Vector3 point in line) {

			map[(int)point.x, (int)point.y, (int)point.z] = new Room(point);
			map[(int)point.x, (int)point.y, (int)point.z].setType(Dungeon.Type.DEBUG);
			addedRooms.Add (map[(int)point.x, (int)point.y, (int)point.z]);
			Debug.Log (point.ToString ());

		}

		updateRoomlist = true;

	}

	List<Vector3> GetLine(Vector3 from, Vector3 to) {

		List<Vector3> line = new List<Vector3> ();

		int x, y, z, dx, dy, dz, l, m, n, x_inc, y_inc, z_inc, err_1, err_2, dx2, dy2, dz2;

		x = (int)from.x;
		y = (int)from.y;
		z = (int)from.z;

		dx = (int)to.x - (int)from.x;
		dy = (int)to.y - (int)from.y;
		dz = (int)to.z - (int)from.z;

		x_inc = Math.Sign (dx);
		y_inc = Math.Sign (dy);
		z_inc = Math.Sign (dz);

		l = Math.Abs (dx);
		m = Math.Abs (dy);
		n = Math.Abs (dz);

		dx2 = l << 1;
		dy2 = m << 1;
		dz2 = n << 1;

		if (l >= m && l >= n) {

			err_1 = dy2 - l;
			err_2 = dz2 - l;
			for (int i = 0; i < l; i++) {

				if (i > 0)
					line.Add (new Vector3 (x, y, z));

				if (err_1 > 0) {

					y += y_inc;
					err_1 -= dx2;

				}

				if (err_2 > 0) {
					
					z += z_inc;
					err_2 -= dx2;
					
				}

				err_1 += dy2;
				err_2 += dz2;

				x += x_inc;

			}

		} else if (m >= l && m >= n) {

			err_1 = dx2 - m;
			err_2 = dz2 - m;
			for (int i = 0; i < m; i++) {
				
				if (i > 0)
					line.Add (new Vector3 (x, y, z));
				
				if (err_1 > 0) {
					
					x += x_inc;
					err_1 -= dy2;
					
				}
				
				if (err_2 > 0) {
					
					z += z_inc;
					err_2 -= dy2;
					
				}
				
				err_1 += dx2;
				err_2 += dz2;
				
				y += y_inc;
				
			}

		} else {

			err_1 = dy2 - n;
			err_2 = dz2 - n;
			for (int i = 0; i < l; i++) {
				
				if (i > 0)
					line.Add (new Vector3 (x, y, z));
				
				if (err_1 > 0) {
					
					y += y_inc;
					err_1 -= dz2;
					
				}
				
				if (err_2 > 0) {
					
					x += x_inc;
					err_2 -= dz2;
					
				}
				
				err_1 += dy2;
				err_2 += dx2;
				
				z += z_inc;
				
			}

		}

		return line;

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

		while (--n > 0) {

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

	List<List<Room>> GetInterConnectedRooms () {

		List<List<Room>> interConnectedRooms = new List<List<Room>> ();
		Flags[,,] mapFlags = new Flags[width, depth, height];

		for (int x = 0; x < width; x++) {
			
			for (int y = 0; y < depth; y++) {
				
				for (int z = 0; z < height; z++) {

					if(mapFlags[x, y, z] != Flags.Done && rooms.Contains (map[x, y, z])) {

						List<Room> connectedRooms = GetRoomsInConnection(x, y, z);
						interConnectedRooms.Add (connectedRooms);

						foreach (Room room in connectedRooms) {

							mapFlags[(int)room.getPosition().x, (int)room.getPosition().y, (int)room.getPosition().z] = Flags.Done;

						}

					}

				}

			}

		}

		return interConnectedRooms;

	}

	List<Room> GetRoomsInConnection (int x, int y, int z) {

		List<Room> roomsList = new List<Room> ();
		Flags[,,] mapFlags = new Flags[width, depth, height];

		Queue<Room> queue = new Queue<Room> ();
		queue.Enqueue (map [x, y, z]);
		mapFlags [x, y, z] = Flags.Done;

		while (queue.Count > 0) {

			Room room = queue.Dequeue ();
			roomsList.Add (room);

			for (int nX = (int)room.getPosition ().x; nX <= room.getPosition ().x + 1; nX++) {
				
				for (int nY = (int)room.getPosition ().y; nY <= room.getPosition ().y + 1; nY++) {
					
					for (int nZ = (int)room.getPosition ().z; nZ <= room.getPosition ().z + 1; nZ++) {

						if (IsInMapRange(nX, nY, nZ) && (nX == (int)room.getPosition ().x || nY == (int)room.getPosition ().y || nZ == (int)room.getPosition ().z)){

							if (mapFlags[nX, nY, nZ] != Flags.Done && rooms.Contains (map[nX, nY, nZ])) {

								mapFlags[nX, nY, nZ] = Flags.Done;
								queue.Enqueue (map[nX, nY, nZ]);

							}

						}

					}

				}
	
			}
		
		}

		return roomsList;

	}

	bool IsInMapRange (int x, int y, int z) {
								
		return x >= 0 && x < width && y >= 0 && y < depth && z >= 0 && z < height;

	}													                                                                

	void OnDrawGizmos () {
		
		if (map != null) {
			
			for (int x = 0; x < width; x++) {
				
				for (int y = 0; y < depth; y++) {
					
					for (int z = 0; z < height; z++) {

						if (map [x, y, z] != null) {

							Vector3 pos = new Vector3 (((-width / 2f + x) * roomSize * spacing) + roomSize * spacing / 2f, ((-depth / 2f + y) * roomSize * spacing) + roomSize * spacing / 2f, ((-height / 2f + z) * roomSize * spacing) + roomSize * spacing / 2f);
							if(!map[x, y, z].TypeOf(Dungeon.Type.DEBUG))
								Gizmos.color = new Color(1, 0, 0, .40f);
							else
								Gizmos.color = Color.gray;
							Gizmos.DrawCube (pos, new Vector3 (roomSize, roomSize, roomSize));

						}

					}
					
				}
				
			}
			
		}
		
	}
	
}

