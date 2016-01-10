using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BoidNode : System.Object{

	public override bool Equals(System.Object obj)
	{
		// If parameter is null return false.
		if (obj == null)
		{
			return false;
		}
		
		// If parameter cannot be cast to Point return false.
		BoidNode b = obj as BoidNode;
		if ((System.Object)b == null)
		{
			return false;
		}
		
		// Return true if the fields match:
		return (b.id == this.id);
	}

	public int GetHashCode(BoidNode n){
		return n.id;
	}

	public int id;
	private static int MAX_ID = 0;
	public static int nodeCount
	{
		get { return MAX_ID; }
	}

	public float hardRadius = 1f;
	public float softRadius = 1.5f;
	public float visibilityRadius = 2f;

	// Terrain
	public bool obstacle;
	public float penalty;
	public Node node;

	// Unit
	public Vector3 position;
	public Vector3 velocity;
	public Vector3 steering;
	public Vector3 aheadVector;
	public float sight = 2;
	
	public float maxSpeed = 1;
	public float mass = 20;
	public float maxForce = 1;

	public float maxSeeAhead = 1f;
	public float maxAvoidForce = 1;


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
		mass = _radius * _radius * 20;
	}

	// TERRAIN
	// This is a waypoint
	public BoidNode (Node _node, float _penalty):this(){
		node = _node;
		position = _node.worldPosition;
		penalty = _penalty;
		hardRadius = 0;
		softRadius = 0.5f;
		visibilityRadius = float.MaxValue;
	}
	//This is a solid object
	public BoidNode(Node _node, float _radius, float _penalty, bool _obstacle = false):this(){
		node = _node;
		position = _node.worldPosition;
		penalty = _penalty;
		hardRadius = _radius;
		softRadius = _radius * 1.5f;
		visibilityRadius = _radius * 2f;
		obstacle = _obstacle;
	}
	public float DistanceFromBorder(BoidNode other){
		float distance = Vector3.Distance (other.position, this.position);
		return distance - this.hardRadius;
	}
	public float DistanceBetweenBorders(BoidNode other){
		float distance = Vector3.Distance (other.position, this.position);
		return distance - this.hardRadius - other.hardRadius;
	}
	public float DistanceFromBorder(Vector3 otherPosition){
		float distance = Vector3.Distance (otherPosition, this.position);
		return distance - this.hardRadius;
	}
	public float DistanceFromBorder(Vector3 thisPosition,Vector3 otherPosition){
		float distance = Vector3.Distance (otherPosition, thisPosition);
		return distance - this.hardRadius;
	}


	public Vector3 predictPosition(float deltaTime){
		return this.position + this.velocity * deltaTime;
	}
	public bool WillCollide(BoidNode other, out Vector3 avoidanceSpeed, out Vector3 collisionPoint, int subSamples = 3){
		if (this != other) {

			aheadVector = velocity.normalized * maxSeeAhead * velocity.magnitude / maxSpeed * Time.deltaTime;
			float distance = float.MaxValue;
			for (int sample = 0; sample <= subSamples; sample++) {
				Vector3 aheadPosition = position + aheadVector * ((float)sample) / subSamples; 
				float currentDistance = other.DistanceFromBorder (aheadPosition) - this.hardRadius;
				if (currentDistance <= 0) {
					distance = currentDistance;
					collisionPoint = aheadPosition;
					avoidanceSpeed = collisionPoint - other.position;
					avoidanceSpeed = -avoidanceSpeed.normalized * maxAvoidForce;
					return true;
				} else if (currentDistance < distance) {
					distance = currentDistance;
				}
			}
		}
		avoidanceSpeed = Vector3.zero;
		collisionPoint = Vector3.zero;
		return false;
	}

	bool IsMoving ()
	{
		return this.velocity.magnitude > 0;
	}

	Vector3 InterceptPosition (BoidNode other, out float distance)
	{
		Vector3 otherPosition = other.position;
		distance = DistanceFromBorder (other);
		float currentSpeed = velocity.magnitude;
		if (other.IsMoving () && currentSpeed > 0) {
			//  See where the other will be when this one arrives to that point)
			otherPosition = other.predictPosition (distance / currentSpeed);
			//  from now on, the target position is that one, so we need to update the distance
			distance = DistanceFromBorder (otherPosition);
		}
		return otherPosition;
	}

	public Vector3 ForceForExpectedVelocity(Vector3 desiredVelocity){ 
		return desiredVelocity - this.velocity;
	}
	public Vector3 MaxSpeed(Vector3 orientedVector){
		return orientedVector.normalized * this.maxSpeed;
	}

	public Vector3 VelocityTowards(BoidNode other){
		
		float distance;
		Vector3 otherPosition = InterceptPosition (other, out distance);
		Vector3 desiredVelocity = MaxSpeed(otherPosition - this.position);

		//Arrival
		if(distance < other.softRadius && distance >= other.hardRadius){
			desiredVelocity *= ((distance - other.hardRadius) / (other.softRadius-other.hardRadius));
		}
		if (distance < other.hardRadius) {
			desiredVelocity = -velocity;
		}
		return desiredVelocity;
	}
	public Vector3 VelocityTowards(Vector3 otherPosition, float otherRadius, float arrivalRadius){
		
		float distance = DistanceFromBorder(otherPosition) - otherRadius;
		Vector3 desiredVelocity = MaxSpeed(otherPosition - this.position);
		
		//Arrival
		if (distance < arrivalRadius) {
			desiredVelocity = (desiredVelocity-velocity)*((distance - otherRadius) / (arrivalRadius - otherRadius));
		}
		if (distance < otherRadius) {
			desiredVelocity = -desiredVelocity;
		}
		return desiredVelocity;
	}


	public Vector3 VelocityAwayFrom(BoidNode other){

		float distance;
		Vector3 otherPosition = InterceptPosition (other, out distance);
		Vector3 desiredVelocity = MaxSpeed(-(otherPosition - this.position));
		return desiredVelocity;
	}
}
[System.Serializable]
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