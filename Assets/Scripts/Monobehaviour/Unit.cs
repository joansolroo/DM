using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public interface UnitInterface {
	void MoveTo ();
	void Wander ();
	void Chase ();
	void Flee ();
	void Patrol ();
	void FollowLeader ();
	void JoinFormation ();

	void Attack ();
	void UseAbility ();
	void GetDamage ();
}
public class Formation{
	Flocker flocker;

	Vector3 Centroid;
	BoidNode[] units;
	Vector3[] unitRelativePositions;

}

public class Unit : BoidController {

	public Transform target;
	public BoidNode targetBoid;
	
	int targetIndex;
	Node[] path;

	//Debug 
	Node node;
	Flocker flock;
	public Node[] neighbourTerrain;
	public BoidNode[] neighbourUnits;

	void Awake(){
		Initialize ();
	}
	protected override void Initialize(){
		boid = new BoidNode (transform.position, radius);
		transform.localScale = Vector3.one * radius * 2;
		flock = new Flocker ();
	}
	void Start() {
		Grid.instance.RegisterUnit(boid);
		targetBoid = target.GetComponent<Unit>().boid;
	}

	void Update(){
	}
	public void MoveTo(Vector3 position){
		PathRequestManager.RequestPath(boid.position,position, OnPathFound);
	}

	public void chase(BoidNode targetBoid){
		PathRequestManager.RequestPath(boid.position,targetBoid.position, OnPathFound);
	}

	public void OnPathFound(Node[] newPath, bool pathSuccessful) {
		if (pathSuccessful) {
			path = newPath;
			StopCoroutine("FollowPath");
			StartCoroutine("FollowPath");
		}
	}

	void Wander ()
	{
		// TODO move to a random place
		throw new System.NotImplementedException ();
	}

	Vector3 ComputeSteeringFromObstacles (Node node, float maxRadius = 1, float maxObstacleForce = 10f)
	{
		// Environment influence
		// take into account the terrain close to the cell
		Vector3 steeringFromSurroundings = new Vector3 ();
		neighbourTerrain = Grid.instance.GetTerrainNeighbours (node, false, maxRadius);
		float currentDistance = float.MaxValue;
		foreach (Node neighbour in neighbourTerrain) {
			if(!neighbour.walkable){
				Vector3 collisionPoint;
				Vector3 terrainSpeed;
				if (neighbour.boidNode.WillCollide (boid, out terrainSpeed, out collisionPoint)) {
					float distance = Vector3.Distance (boid.position, collisionPoint);
					if (distance < currentDistance) {
						currentDistance = distance;
						steeringFromSurroundings = terrainSpeed;;
					}
				}
			}
		}
		if(currentDistance < float.MaxValue)
			steeringFromSurroundings = steeringFromSurroundings * maxObstacleForce * Mathf.Max (0, 1f - currentDistance/maxRadius);

		return steeringFromSurroundings;
	}

	void MoveTo(BoidNode currentWaypoint){
		node = boid.node;
		
		// Influence of next checkpoint
	    Vector3 steeringFromTarget = boid.VelocityTowards(currentWaypoint.position,0,0.5f);

		neighbourUnits = Grid.instance.GetUnitNeighbours (boid, false, 3);
		Vector3 steeringFromUnits = Flocker.ComputeSeparationForce(boid, neighbourUnits,radius,1);

		Vector3 steeringFromSurroundings = ComputeSteeringFromObstacles(node,1,10);
		// Compose
		Vector3 steering = Vector3.ClampMagnitude (steeringFromTarget + steeringFromUnits+steeringFromSurroundings, steeringFromTarget.magnitude);
		steering = steering / boid.mass;

		// Floor influence
		float movementRatio = 1-node.boidNode.penalty;

		boid.velocity = Vector3.ClampMagnitude (boid.velocity +steering, boid.maxSpeed*movementRatio);

		Grid.instance.RemoveUnit(boid);
		boid.position = boid.predictPosition(Time.deltaTime);
		Grid.instance.RegisterUnit(boid);
		transform.position = boid.position;
	}

	IEnumerator FollowPath() {
		if(path.Length == 0)
			yield break;
		targetIndex = 0;
		boid.position = transform.position;
		Node currentWaypoint =  path[targetIndex];

		while (true) {
			float distanceToWaypoint = Vector3.Distance(boid.position,currentWaypoint.worldPosition);
			bool updated = false;
			if ((targetIndex <= path.Length - 2 && distanceToWaypoint < currentWaypoint.boidNode.softRadius)
			    ||(targetIndex == path.Length - 1 && distanceToWaypoint <= (currentWaypoint.boidNode.softRadius-currentWaypoint.boidNode.hardRadius)*0.5+currentWaypoint.boidNode.hardRadius)) {
				targetIndex ++;
				if (targetIndex >= path.Length) {
					if(boid.position != targetBoid.position)
						chase(targetBoid);
					yield break;
				}
				updated = true;
			}
			if(updated){
				//PathRequestManager.RequestPath(boid.position,target.position, OnPathUpdated);
				currentWaypoint = path[targetIndex];

			}
			MoveTo(currentWaypoint.boidNode);
			yield return null;
		}
	}

	public void OnDrawGizmos() {
		//if (!ShowGizmos) {
		//	return;
		//}
		base.OnDrawGizmos();
		GUIStyle style = new GUIStyle();

		if (path != null) {
			for (int i = targetIndex; i < path.Length; i ++) {
				float cidx = 1;//((float)boid.id)/(BoidNode.nodeCount);
				Color c =  Color.black;//Color.Lerp(Color.yellow,Color.red,cidx);
				Handles.color = c;
				Handles.DrawWireDisc(path [i].worldPosition , Vector3.up, path [i].boidNode.hardRadius*cidx); 
				Handles.DrawWireDisc(path [i].worldPosition , Vector3.up, path [i].boidNode.softRadius*cidx);
				Gizmos.color = c;
				if (i == targetIndex) {
					style.normal.textColor = c;
					Handles.Label(path [i].worldPosition,""+boid.DistanceBetweenBorders(path [i].boidNode).ToString("0.00"),style);
					Gizmos.DrawLine (transform.position, path [i].worldPosition);
				} else {
					Gizmos.DrawLine (path [i - 1].worldPosition, path [i].worldPosition);
				}
			}
		}

		if (node != null) {
			Gizmos.DrawLine (transform.position, node.worldPosition);
		}

		style.normal.textColor = Color.white;
		if (neighbourTerrain != null){
			foreach (Node neighbour in neighbourTerrain) {
				if (!neighbour.walkable){

					Vector3 avoidanceSpeed;
					Vector3 collisionPoint;
					Color c;
					if(boid.WillCollide(neighbour.boidNode,out avoidanceSpeed, out collisionPoint))
						c = Color.red;
					else
						c = Color.white;
					Gizmos.color = c;
					Gizmos.DrawLine (transform.position, neighbour.worldPosition);
					style.normal.textColor = c;
					Handles.Label((transform.position+neighbour.worldPosition)/2,""+boid.DistanceBetweenBorders(neighbour.boidNode).ToString("0.00"),style);
				}
			}
		}
		if (neighbourUnits != null){
			foreach (BoidNode neighbour in neighbourUnits) {
				//if (!neighbour.walkable) 
				Color c = Color.Lerp(Color.blue,Color.cyan,((float)boid.id)/BoidNode.nodeCount);
				Handles.color = c;
				Handles.DrawDottedLine (transform.position, neighbour.position,1f);
				//Handles.color = c;
				style.normal.textColor = c;
				Handles.Label((transform.position*0.75f+neighbour.position*0.25f),""+boid.DistanceBetweenBorders(neighbour).ToString("0.00"),style);
			}
		}
	}
}
