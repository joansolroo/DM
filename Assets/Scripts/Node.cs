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

	public BoidNode(Node _node){
		node = _node;
		
		penalty = 0;
		hardRadius = 0;
		softRadius = 0;
		visibilityRadius = float.MaxValue;
	}

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
	public static int MaxMovementPenalty = 20;

	public PathfindingNode pathfindingNode;
	public BoidNode boidNode;
	
	public Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty) {
		
		walkable = _walkable;
		worldPosition = _worldPos;
		gridX = _gridX;
		gridY = _gridY;
		movementPenalty = _penalty;


		pathfindingNode = new PathfindingNode (this);

		if (walkable)
			boidNode = new BoidNode (this);
		else
			boidNode = new BoidNode (this, 0.5f, movementPenalty/MaxMovementPenalty);
	}
}