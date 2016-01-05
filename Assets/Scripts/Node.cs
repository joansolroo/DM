using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoidNode{

	public class Comparer : IEqualityComparer<BoidNode>{
		public bool Equals(BoidNode n1, BoidNode n2){
			return n1.Equals (n2);
		}
		public int GetHashCode(BoidNode n){
			return n.id;
		}
	}

	public int id;
	private static int MAX_ID = 0;

	public float hardRadius = 1f;
	public float softRadius = 1.5f;
	public float visibilityRadius = 2f;

	// Terrain
	public bool obstacle;
	public float penalty;
	public Node other;

	// Unit
	public Vector3 position;
	public Vector3 velocity;
	public Vector3 steering;
	public Vector3 aheadVector;
	public float sight = 2;
	
	public float maxSpeed = 1;
	public float mass = 20;
	public float maxForce = 1;

	public float maxSeeAhead = 2f;
	public float maxAvoidForce = 5;


	public BoidNode (){

		this.id = ++MAX_ID;
		position = new Vector3();
		velocity = new Vector3();
		steering = new Vector3();
	}
	//This is a unit
	public BoidNode(Vector3 _position, float _radius):this(){
		position = _position;

		hardRadius = _radius;
		softRadius = _radius * 1.5f;
		visibilityRadius = _radius * 2f;
		sight = visibilityRadius;
		obstacle = true;
	}

	// TERRAIN
	// This is a waypoint
	public BoidNode (Node _node, float _penalty):this(){
		other = _node;
		position = _node.worldPosition;
		penalty = _penalty;
		hardRadius = 0;
		softRadius = 0.5f;
		visibilityRadius = float.MaxValue;
	}
	//This is a solid object
	public BoidNode(Node _node, float _radius, float _penalty, bool _obstacle = false):this(){
		other = _node;
		position = _node.worldPosition;
		penalty = _penalty;
		hardRadius = _radius;
		softRadius = _radius * 1.5f;
		visibilityRadius = _radius * 2f;
		obstacle = _obstacle;
	}
	public float DistanceFromTheBorder(BoidNode other){
		float distance = Vector3.Distance (other.position, this.position);
		return distance - this.hardRadius;
	}
	public float DistanceFromTheBorder(Vector3 otherPosition){
		float distance = Vector3.Distance (otherPosition, this.position);
		return distance - this.hardRadius;
	}
	public float DistanceFromTheBorder(Vector3 thisPosition,Vector3 otherPosition){
		float distance = Vector3.Distance (otherPosition, thisPosition);
		return distance - this.hardRadius;
	}


	public Vector3 predictPosition(float deltaTime){
		return this.position + this.velocity * deltaTime;
	}
	public bool WillCollide(BoidNode other, out Vector3 avoidanceForce, out Vector3 collisionPoint){

		aheadVector = velocity.normalized * maxSeeAhead* velocity.magnitude / maxSpeed;
		Vector3 aheadT2 = position + aheadVector * Time.deltaTime;
		Vector3 aheadT1 = position + aheadVector * Time.deltaTime * 0.5f;

		if (other.DistanceFromTheBorder (position) <= this.hardRadius) {
			collisionPoint = position;
			avoidanceForce = collisionPoint - other.position;
			avoidanceForce = -avoidanceForce.normalized * maxAvoidForce;

			return true;
		} else if (other.DistanceFromTheBorder (aheadT1) <= this.hardRadius) {
			collisionPoint = aheadT1;
			avoidanceForce = collisionPoint - other.position;
			avoidanceForce = -avoidanceForce.normalized * maxAvoidForce;

			return true; 
		} else if (other.DistanceFromTheBorder (aheadT2) <= this.hardRadius) {
			collisionPoint = aheadT2;
			avoidanceForce = collisionPoint - other.position;
			avoidanceForce = -avoidanceForce.normalized * maxAvoidForce;
			return true;
		} else {
			avoidanceForce = Vector3.zero;
			collisionPoint = Vector3.zero;
			return false;
		}

	}
	public Vector3 computeInfluence(BoidNode other){
		
		Vector3 otherPosition = other.position;
		float distance = DistanceFromTheBorder (other);
		if(other.velocity.magnitude>0){
		//  See where the other will be when this one arrives to that point)
			otherPosition = other.predictPosition(distance / maxSpeed);
		//  from now on, the target position is that one, so we need to update the distance
			distance = DistanceFromTheBorder (otherPosition);
		}
		Vector3 desiredVelocity = (otherPosition - this.position).normalized*this.maxSpeed;
		
		if (distance < other.visibilityRadius) { 
			if (other.obstacle || distance < other.hardRadius)
				// FLEE
				desiredVelocity = -desiredVelocity;
			else if(distance < other.softRadius && distance >= other.hardRadius)
				// ARRIVE
				desiredVelocity *= ((distance - other.hardRadius) / (other.softRadius-other.hardRadius));
			else{
				// JUST MOVE THERE
			}
			this.steering = desiredVelocity - this.velocity;
		}
		return this.steering;
	}

}
public class Node {
	public bool walkable;
	public Vector3 worldPosition;
	public int gridX;
	public int gridY;

	public int movementPenalty;
	public static int MaxMovementPenalty = 5;

	public PathfindingNode pathfindingNode;
	public BoidNode boidNode;
	
	public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty) {
		
		walkable = _walkable;
		worldPosition = _worldPos;
		gridX = _gridX;
		gridY = _gridY;
		movementPenalty = _penalty;


		pathfindingNode = new PathfindingNode (this);

		float normalizedPenalty = ((float)movementPenalty) / MaxMovementPenalty;
		if (walkable)
			boidNode = new BoidNode (this, normalizedPenalty);
		else {
			float radius = 0.5f;
			boidNode = new BoidNode (this, radius, normalizedPenalty, true);
			//boidNode.softRadius = radius;
			//boidNode.visibilityRadius = radius;
		}
	}
}