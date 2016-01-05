using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class Unit : Boid {


	public Transform target;
	
	int targetIndex;
	Node[] path;

	//Debug 
	Node node;
	Node[] neighbourTerrain;
	BoidNode[] neighbourUnits;
	
	void Awake(){
		Initialize ();
	}
	protected override void Initialize(){
		boid = new BoidNode (transform.position, radius);
	}
	void Start() {
		Grid.instance.RegisterUnit(boid);
		chase (target);
	}
	void Update(){
		transform.position = boid.position;
	}

	public void chase(Transform target){
		PathRequestManager.RequestPath(transform.position,target.position, OnPathFound);
	}
	public void OnPathFound(Node[] newPath, bool pathSuccessful) {
		if (pathSuccessful) {
			path = newPath;
			StopCoroutine("FollowPath");
			StartCoroutine("FollowPath");
		}
	}
	float ManhattanDistance(Vector3 origin, Vector3 destiny){
		Vector3 difference = origin-destiny;
		return Mathf.Abs (difference.x) + Mathf.Abs (difference.y) + Mathf.Abs (difference.z);
	}

	IEnumerator FollowPath() {
		if(path.Length == 0)
			yield break;
		targetIndex = 0;
		boid.position = transform.position;
		Node currentWaypoint =  path[targetIndex];

		while (true) {
			float distanceToWaypoint = Vector3.Distance(boid.position,currentWaypoint.worldPosition);

			if ((targetIndex <= path.Length - 2 && distanceToWaypoint < currentWaypoint.boidNode.softRadius)
			    ||(targetIndex == path.Length - 1 && distanceToWaypoint <= (currentWaypoint.boidNode.softRadius-currentWaypoint.boidNode.hardRadius)*0.1+currentWaypoint.boidNode.hardRadius)) {
				targetIndex ++;
				if (targetIndex >= path.Length) {
					if(transform.position != target.position)
						chase(target);
					yield break;
				}
				currentWaypoint = path[targetIndex];
			}
			node = Grid.instance.NodeFromWorldPoint(boid.position);

			// Influence of next checkpoint
			Vector3 steeringFromTarget = boid.computeInfluence(currentWaypoint.boidNode);
			// Environment influence
			// take into account the terrain close to the cell
			Vector3 steeringFromSurroundings = new Vector3();
			neighbourTerrain = Grid.instance.GetTerrainNeighbours (node, false,2);
			float currentDistance = float.MaxValue;
			foreach (Node neighbour in neighbourTerrain){
				Vector3 collisionPoint;
				Vector3 terrainForce;
				if(neighbour.boidNode.WillCollide(boid,out terrainForce, out collisionPoint))
				{
					float distance = Vector3.Distance(boid.position,collisionPoint);
					if(distance < currentDistance)
					{
						steeringFromSurroundings = terrainForce;
						currentDistance = distance;
						print ("COLLISION PREDICTED1");
					}
				}
					//steeringFromSurroundings += boid.computeInfluence(neighbour.boidNode);
			}
			// Units influence
			Vector3 steeringFromUnits = new Vector3();
			neighbourUnits = Grid.instance.GetUnitNeighbours (boid, false,2);
			currentDistance = float.MaxValue;
			foreach (BoidNode neighbour in neighbourUnits){
				Vector3 collisionPoint;
				Vector3 unitForce;
				if(neighbour.WillCollide(boid,out unitForce, out collisionPoint))
				{
					float distance = Vector3.Distance(boid.position,collisionPoint);
					if(distance < currentDistance)
					{
						steeringFromUnits = unitForce;
						currentDistance = distance;
						print ("COLLISION PREDICTED2");

					}
				}
				//steeringFromSurroundings += boid.computeInfluence(neighbour.boidNode);
			}


			/*foreach (BoidNode neighbour in neighbourUnits){
				steeringFromUnits += boid.computeInfluence(neighbour);
			}*/
			// Floor influence
			float movementRatio = 1-node.boidNode.penalty;

			// Compose
			Vector3 steering = Vector3.ClampMagnitude (steeringFromTarget+steeringFromSurroundings*5+steeringFromUnits, boid.maxForce);
			steering = steering / boid.mass;

			boid.velocity = Vector3.ClampMagnitude (boid.velocity + steering , boid.maxSpeed*movementRatio);
			Grid.instance.RemoveUnit(boid);
			boid.position = boid.predictPosition(Time.deltaTime);
			Grid.instance.RegisterUnit(boid);
			yield return null;
		}
	}

	public void OnDrawGizmos() {
		//if (!ShowGizmos) {
		//	return;
		//}
		base.OnDrawGizmos ();
		if (path != null) {
			for (int i = targetIndex; i < path.Length; i ++) {

				Handles.color = Color.black;
				Handles.DrawWireDisc(path [i].worldPosition , Vector3.up, path [i].boidNode.hardRadius); 
				Handles.DrawWireDisc(path [i].worldPosition , Vector3.up, path [i].boidNode.softRadius);
				Gizmos.color = Color.black;
				if (i == targetIndex) {
					Gizmos.DrawLine (transform.position, path [i].worldPosition);
				} else {
					Gizmos.DrawLine (path [i - 1].worldPosition, path [i].worldPosition);
				}
			}
		}

		if (node != null) {
			Gizmos.DrawLine (transform.position, node.worldPosition);
		}
		if (neighbourTerrain != null){
			foreach (Node neighbour in neighbourTerrain) {
				if (!neighbour.walkable) 
					Gizmos.DrawLine (transform.position, neighbour.worldPosition);
			}
		}
		if (neighbourUnits != null){
			Gizmos.color = Color.cyan;
			foreach (BoidNode neighbour in neighbourUnits) {
				//if (!neighbour.walkable) 
					Gizmos.DrawLine (transform.position, neighbour.position);
			}
		}
	}
}
