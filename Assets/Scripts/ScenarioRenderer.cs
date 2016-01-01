using UnityEngine;
using System.Collections;

public class ScenarioRenderer : MonoBehaviour {

	public Transform prefab;

	public GameObject BoardController;
	
	Grid grid;
	
	void Awake() {
		grid = GetComponent<Grid>();

	}
	/* Render procedurally
	void Start() {
		foreach (Node n in grid.grid) {
			if(!n.walkable)
				Instantiate(prefab, n.worldPosition, Quaternion.identity);
		}
	}
	*/
}
