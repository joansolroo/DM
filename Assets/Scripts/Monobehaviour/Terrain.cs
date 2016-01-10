using UnityEngine;
using System.Collections;

public class Terrain : BoidController {
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	protected override void Initialize(){
		boid = new BoidNode (transform.position, radius);
	}
}
