using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour {


	public Transform target;

	Vector3 position;
	Vector3 velocity;
	Vector3 steering;

	public float maxSpeed = 1;
	public float mass = 20;
	public float maxForce = 1;

	public float sight = 2;
	Node[] path;
	int targetIndex;

	//Debug 
	Node node;
	List<Node> neighbours;
	
	void Start() {
		chase (target);
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

	Vector3 computeInfluence(Node node){
		Vector3 desiredVelocity = (node.worldPosition - position).normalized*maxSpeed;

		float distance = Vector3.Distance (node.worldPosition, position);
		if (distance < node.boidNode.visibilityRadius) { 
			if (! node.walkable || distance < node.boidNode.hardRadius)
				desiredVelocity = -desiredVelocity;
			else if(distance < node.boidNode.softRadius && distance >= node.boidNode.hardRadius)
				desiredVelocity *= ((distance - node.boidNode.hardRadius) / (node.boidNode.softRadius-node.boidNode.hardRadius));

			steering = desiredVelocity - velocity;
		}
		return steering;
	}

	IEnumerator FollowPath() {
		if(path.Length== 0)
			yield break;
		targetIndex = 0;
		position = transform.position;
		Node currentWaypoint =  path[targetIndex];

		while (true) {
			float distanceToWaypoint = Vector3.Distance(position,currentWaypoint.worldPosition);

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
			node = Grid.instance.NodeFromWorldPoint(position);

			// Influence of next checkpoint
			Vector3 steeringFromTarget = computeInfluence(currentWaypoint);
			// Environment influence
			// take into account the terrain close to the cell
			Vector3 steeringFromSurroundings = new Vector3();
			neighbours = Grid.instance.GetNeighbours (node, false,sight);
			foreach (Node neighbour in neighbours){
				if(!neighbour.walkable) 
					steeringFromSurroundings += computeInfluence(neighbour);
			}

			// Floor influence
			float movementRatio = 1-node.boidNode.penalty;

			// Compose
			Vector3 steering = Vector3.ClampMagnitude (steeringFromTarget+steeringFromSurroundings, maxForce);
			steering = steering / mass;

			velocity = Vector3.ClampMagnitude (velocity + steering , maxSpeed*movementRatio);

			//velocity = desiredVelocity;
			position = position + velocity*Time.deltaTime;//Vector3.MoveTowards(transform.position,currentWaypoint,speed * Time.deltaTime);
			transform.position = position;
			yield return null;
		}
	}

	public void OnDrawGizmos() {
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
		Gizmos.color = Color.blue;
		Gizmos.DrawLine (transform.position, transform.position + velocity);
		Gizmos.color = Color.red;
		Gizmos.DrawLine (transform.position, transform.position + steering);

		//Visibility
		Gizmos.color = Color.white;
		//Gizmos.DrawWireSphere (transform.position, sight);
		Handles.color = Color.white;
		Handles.DrawWireDisc(transform.position , Vector3.up, sight); 
		if (node != null) {
			Gizmos.DrawLine (transform.position, node.worldPosition);
		}
		if (neighbours != null){
			foreach (Node neighbour in neighbours) {
				if (!neighbour.walkable) 
					Gizmos.DrawLine (transform.position, neighbour.worldPosition);
			}
		}
	}
}
