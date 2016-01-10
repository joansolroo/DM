using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class Unit : BoidController {


	public Transform target;
	
	int targetIndex;
	Node[] path;

	//Debug 
	Node node;
	public Node[] neighbourTerrain;
	public BoidNode[] neighbourUnits;

	public Vector3 steeringFromTarget;
	public Vector3 steeringFromUnits;
	public Vector3 steeringFromSurroundings;

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

	public void OnPathUpdated(Node[] newPath, bool pathSuccessful) {
		if (pathSuccessful) {
			targetIndex = 0;
			path = newPath;
			//StopCoroutine("FollowPath");
			//StartCoroutine("FollowPath");
		}
	}
	float ManhattanDistance(Vector3 origin, Vector3 destiny){
		Vector3 difference = origin-destiny;
		return Mathf.Abs (difference.x) + Mathf.Abs (difference.y) + Mathf.Abs (difference.z);
	}

	void Wander ()
	{
		// TODO move to a random place
		throw new System.NotImplementedException ();
	}
	
	private Vector3 Cohesion(BoidNode[] neighbourUnits){
		Vector3 velocity = new Vector3();
		Vector3 force = new Vector3();
		Vector3 centroid = new Vector3();
		int neighborCount = 0;
		
		float cohesionRadius = 1f;
		float maxCohesion = 0.1f;//separationRadius*1.5f;
		foreach(BoidNode b in neighbourUnits){
			if (b == this.boid)
				continue;
			float distance = this.boid.DistanceBetweenBorders(b);
			if (b != this.boid &&  distance < cohesionRadius) {
				centroid += b.position;
				neighborCount++;
			}
		}
		
		if (neighborCount != 0) {
			centroid = centroid /neighborCount;	
			//print ("Flocking cohesion");
			float distance = this.boid.DistanceFromBorder(centroid);
			velocity = (this.boid.position-centroid)*(distance/cohesionRadius);
			//force = velocity-this.boid.velocity;
			//force = velocity - this.boid.velocity;
			//force.Normalize();
			//force *= maxCohesion;

		}
		return velocity;
	}

	private Vector3 Separation(BoidNode[] neighbourUnits){
		Vector3 force = new Vector3();
		int neighborCount = 0;

		float separationRadius = 0.25f;
		float maxSeparation = 1;
		foreach(BoidNode b in neighbourUnits){
			if (b == this.boid)
				continue;
			float distance = this.boid.DistanceBetweenBorders(b);
			if (b != this.boid &&  distance < separationRadius) {
				force +=  (this.boid.position-b.position);//*(1-distance/separationRadius);
				neighborCount++;
			}
		}

		if (neighborCount != 0) {
			force = force/(neighborCount);	
			//print ("Flocking Separation");

			//force = velocity-this.boid.velocity;
			force.Normalize();
			force *= maxSeparation;
		}
		// Assume all the neigbours will make an equivalent effort
		return force;
	}

	private Vector3 Alignment(BoidNode[] neighbourUnits){
		Vector3 force = new Vector3();
		Vector3 velocity = new Vector3();
		int neighborCount = 0;
		
		float AlignmentRadius = 0.5f;
		float maxAlignment = 0.1f;
		foreach(BoidNode b in neighbourUnits){
			if (b == this.boid)
				continue;
			float distance = this.boid.DistanceBetweenBorders(b);
			if (b != this.boid &&  distance < AlignmentRadius) {
				velocity += b.velocity;
				neighborCount++;
			}
		}
		
		if (neighborCount != 0) {
			//print ("Flocking Alignment");
			velocity = velocity/neighborCount;
			//force = velocity-this.boid.velocity;
			//force.Normalize();
			//force *= maxAlignment;
		}
		
		return velocity*maxAlignment;
	}
	private Vector3 FlockInfluence(){
		neighbourUnits = Grid.instance.GetUnitNeighbours (boid, false, 3);
		float currentDistance = float.MaxValue;
		Vector3 separation = Separation (neighbourUnits);
		Vector3 cohesion = Cohesion (neighbourUnits);
		Vector3 alignment = Alignment (neighbourUnits);
		Vector3 unitForce = separation + cohesion * 0 + alignment *0;
		return unitForce;
	}

	Vector3 ComputeSteeringFromObstacles ()
	{
		// Environment influence
		// take into account the terrain close to the cell
		Vector3 steeringFromSurroundings = new Vector3 ();
		float r = 1f;
		neighbourTerrain = Grid.instance.GetTerrainNeighbours (node, true, r);
		float currentDistance = float.MaxValue;
		foreach (Node neighbour in neighbourTerrain) {
			if(!neighbour.walkable){
				Vector3 collisionPoint;
				Vector3 terrainSpeed;
				if (neighbour.boidNode.WillCollide (boid, out terrainSpeed, out collisionPoint)) {
					float distance = Vector3.Distance (boid.position, collisionPoint);
					if (distance < currentDistance) {
						currentDistance = distance;
						//print ("COLLISION with TERRAIN PREDICTED");
						steeringFromSurroundings = terrainSpeed;
					}
				}
			}
		}

		return steeringFromSurroundings;
	}

	void MoveTowards(BoidNode currentWaypoint){
		node = boid.node;//Grid.instance.NodeFromWorldPoint(boid.position);
		
		// Influence of next checkpoint
	    steeringFromTarget = boid.VelocityTowards(currentWaypoint);
		steeringFromUnits = FlockInfluence();
		steeringFromSurroundings = ComputeSteeringFromObstacles();
		// Compose
		Vector3 steering = Vector3.ClampMagnitude (steeringFromTarget + steeringFromUnits+steeringFromSurroundings*10, boid.maxForce);
		steering = steering / boid.mass;

		// Floor influence
		float movementRatio = 1-node.boidNode.penalty;

		boid.velocity = Vector3.ClampMagnitude (boid.velocity +steering, boid.maxSpeed*movementRatio);

		Grid.instance.RemoveUnit(boid);
		boid.position = boid.predictPosition(Time.deltaTime);
		Grid.instance.RegisterUnit(boid);
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
			    ||(targetIndex == path.Length - 1 && distanceToWaypoint <= (currentWaypoint.boidNode.softRadius-currentWaypoint.boidNode.hardRadius)*0.1+currentWaypoint.boidNode.hardRadius)) {
				targetIndex ++;
				if (targetIndex >= path.Length) {
					if(transform.position != target.position)
						chase(target);
					yield break;
				}
				updated = true;
			}
			if(updated){
				//PathRequestManager.RequestPath(boid.position,target.position, OnPathUpdated);
				currentWaypoint = path[targetIndex];

			}
			MoveTowards(currentWaypoint.boidNode);
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
				float cidx = ((float)boid.id)/(BoidNode.nodeCount%10)%1;
				Color c =  Color.Lerp(Color.yellow,Color.red,cidx);
				Handles.color = c;
				Handles.DrawWireDisc(path [i].worldPosition , Vector3.up, path [i].boidNode.hardRadius*cidx); 
				Handles.DrawWireDisc(path [i].worldPosition , Vector3.up, path [i].boidNode.softRadius*cidx);
				Gizmos.color = c;
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
				if (!neighbour.walkable){
					Gizmos.color = Color.red;
					Gizmos.DrawLine (transform.position, neighbour.worldPosition);
					Handles.color = Color.white;
					Handles.Label((transform.position+neighbour.worldPosition)/2,""+boid.DistanceBetweenBorders(neighbour.boidNode).ToString("0.00"));
				}
			}
		}
		GUIStyle style = new GUIStyle ();
		if (neighbourUnits != null){
			foreach (BoidNode neighbour in neighbourUnits) {
				//if (!neighbour.walkable) 
				Handles.color = Color.Lerp(Color.blue,Color.cyan,((float)boid.id)/BoidNode.nodeCount);
				Handles.DrawDottedLine (transform.position, neighbour.position,1f);
				Handles.color = Color.white;
				Handles.Label((transform.position*0.75f+neighbour.position*0.25f),""+boid.DistanceBetweenBorders(neighbour).ToString("0.00"));
			}
		}
	}
}
