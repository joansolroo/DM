using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Unit : MonoBehaviour {


	public Transform target;

	Vector3 position;
	Vector3 velocity;
	Vector3 steering;

	public float maxSpeed = 0.1f;
	public float mass = 1;
	public float maxForce = 0.1f;

	Node[] path;
	int targetIndex;
	
	void Start() {
		PathRequestManager.RequestPath(transform.position,target.position, OnPathFound);
	}

	public void OnPathFound(Node[] newPath, bool pathSuccessful) {
		if (pathSuccessful) {
			path = newPath;
			StopCoroutine("FollowPath");
			StartCoroutine("FollowPath");
		}
	}
	Vector3 computeInfluence(Node node){
		Vector3 desiredVelocity = (node.worldPosition - position).normalized*maxSpeed;

		float distance = Vector3.Distance (node.worldPosition, position);
		if (distance < node.boidNode.visibilityRadius) { 

			steering = desiredVelocity - velocity;
			if (!node.walkable)
				steering = -steering;
		}
		return steering;
	}
	IEnumerator FollowPath() {

		position = transform.position;
		Node currentWaypoint =  path[0];

		while (true) {
		
			if (Vector3.Distance(position,currentWaypoint.worldPosition)<0.5f) {
				targetIndex ++;
				if (targetIndex >= path.Length) {
					yield break;
				}
				currentWaypoint = path[targetIndex];
			}
			Node node = Grid.instance.NodeFromWorldPoint(position);
			List<Node> neighbours = Grid.instance.GetNeighbours (node, true);

			// Influence of next checkpoint
			Vector3 steering = computeInfluence(currentWaypoint);
			// Environment influence
			// ... take into account the terrain close to the cell
			foreach (Node neighbour in neighbours){
				if(!neighbour.walkable) 
					steering += computeInfluence(neighbour);
			}

			// Floor influence

			steering = Vector3.ClampMagnitude (steering, maxForce);
			steering = steering / mass;

			velocity = Vector3.ClampMagnitude (velocity + steering , maxSpeed);

			//velocity = desiredVelocity;
			position = position + velocity*Time.deltaTime;//Vector3.MoveTowards(transform.position,currentWaypoint,speed * Time.deltaTime);
			transform.position = position;
			yield return null;
		}
	}

	public void OnDrawGizmos() {
		if (path != null) {
			for (int i = targetIndex; i < path.Length; i ++) {
				Gizmos.color = Color.black;
				Gizmos.DrawWireSphere(path[i].worldPosition, 0.5f);
		
				if (i == targetIndex) {
					Gizmos.DrawLine(transform.position, path[i].worldPosition);
				}
				else {
					Gizmos.DrawLine(path[i-1].worldPosition,path[i].worldPosition);
				}
			}
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(transform.position, transform.position + velocity);
		Gizmos.color = Color.red;
		Gizmos.DrawLine(transform.position, transform.position + steering);

		Node node = Grid.instance.NodeFromWorldPoint(transform.position);
		Gizmos.color = Color.white;
		Gizmos.DrawLine(transform.position, node.worldPosition);
		List<Node> neighbours = Grid.instance.GetNeighbours (node, true);

		foreach (Node neighbour in neighbours){
			if(!neighbour.walkable) 
				Gizmos.DrawLine(transform.position, neighbour.worldPosition);
		}

	}
}
