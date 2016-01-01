using UnityEngine;
using System.Collections;

public class BoidNode{
	public float hardRadius = 1f;
	public float softRadius = 1.5f;
	public float visibilityRadius = 2f;
	public float penalty;
	public Node node;

	public Vector3 velocity;
	public Vector3 steering;

	// This is a waypoint
	public BoidNode(Node _node, float _penalty){
		node = _node;
		penalty = _penalty;
		hardRadius = 0;
		softRadius = 0.5f;
		visibilityRadius = float.MaxValue;
	}

	//This is a solid object
	public BoidNode(Node _node, float _radius, float _penalty){
		node = _node;
	
		penalty = _penalty;
		hardRadius = _radius;
		softRadius = _radius * 1.5f;
		visibilityRadius = _radius * 2f;
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
		else
			boidNode = new BoidNode (this, 0.5f, normalizedPenalty);
	}
}