using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

	public static GameManager instance = null;

	public MapGenerator2 mapGen;

	[HideInInspector]
	public int level = 1;

	bool doingSetup;

	void Awake () {

		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy (gameObject);

		DontDestroyOnLoad (gameObject);

		mapGen = GetComponent<MapGenerator2> ();
		InitGame ();

	}

	void InitGame () {

		doingSetup = true;

		mapGen.SetupScene (level);

	}

}
