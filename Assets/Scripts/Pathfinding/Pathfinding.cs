using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public class Pathfinding : MonoBehaviour {
	

	PathRequestManager requestManager;
	public GameObject BoardController;
	public bool includeDiagonals = false;
    Grid grid;

	void Awake() {
		requestManager = GetComponent<PathRequestManager>();
		grid = BoardController.GetComponent<Grid>();
	}
	

	public void StartFindPath(Vector3 startPos, Vector3 targetPos) {
		StartCoroutine(FindPath(startPos,targetPos));
	}

	IEnumerator FindPath(Vector3 startPos, Vector3 targetPos) {

		Stopwatch sw = new Stopwatch();
		sw.Start();

		Node[] waypoints = new Node[0];
		bool pathSuccess = false;

		PathfindingNode startNode = grid.NodeFromWorldPoint(startPos).pathfindingNode;
		PathfindingNode targetNode = grid.NodeFromWorldPoint(targetPos).pathfindingNode;

		if (startNode.node.walkable && targetNode.node.walkable) {
			Heap<PathfindingNode> openSet = new Heap<PathfindingNode>(grid.MaxSize);
			HashSet<PathfindingNode> closedSet = new HashSet<PathfindingNode>();
			openSet.Add(startNode);

			while (openSet.Count > 0) {
				PathfindingNode currentNode = openSet.RemoveFirst();
				closedSet.Add(currentNode);

				if (currentNode == targetNode) {
					sw.Stop();
					print ("Path found: " + sw.ElapsedMilliseconds + " ms");
					pathSuccess = true;
					break;
				}

				foreach (Node neighbourNode in grid.GetTerrainNeighbours(currentNode.node, includeDiagonals)) {
					PathfindingNode neighbour = neighbourNode.pathfindingNode;
					if (!neighbour.node.walkable || closedSet.Contains(neighbour)) {
						continue;
					}

					int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.node.movementPenalty;
					if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
						neighbour.gCost = newMovementCostToNeighbour;
						neighbour.hCost = GetDistance(neighbour, targetNode);
						neighbour.parent = currentNode;

						if (!openSet.Contains(neighbour))
							openSet.Add(neighbour);
						else
							openSet.UpdateItem(neighbour);
					}
				}
			}
		}
		yield return null;
		if (pathSuccess) {
			waypoints = RetracePath(startNode,targetNode);
		}
		requestManager.FinishedProcessingPath(waypoints,pathSuccess);

	}

	Node[] RetracePath(PathfindingNode startNode, PathfindingNode endNode) {
		List<PathfindingNode> path = new List<PathfindingNode>();
		PathfindingNode currentNode = endNode;

		while (currentNode != startNode) {
			path.Add(currentNode);
			currentNode = currentNode.parent;
		}
		Node[] waypoints = SimplifyReversedPath(path, true);
		Array.Reverse(waypoints);
		return waypoints;

	}

	Node[] SimplifyReversedPath(List<PathfindingNode> path, bool simplify) {
		List<Node> waypoints = new List<Node>();
		Vector2 directionOld = Vector2.zero;

		for (int i = 0; i < path.Count-1; i ++) {
			Vector2 directionNew;

			directionNew = new Vector2(path[i+1].node.gridX - path[i].node.gridX,path[i+1].node.gridY - path[i].node.gridY);

			if (!simplify || directionNew != directionOld) {
				waypoints.Add(path[i].node);
			}
			directionOld = directionNew;
		}
		waypoints.Add(path[path.Count-1].node);
		return waypoints.ToArray();
	}

	int GetDistance(PathfindingNode nodeA, PathfindingNode nodeB) {
		int dstX = Mathf.Abs(nodeA.node.gridX - nodeB.node.gridX);
		int dstY = Mathf.Abs(nodeA.node.gridY - nodeB.node.gridY);

		if (dstX > dstY)
			return 14*dstY + 10* (dstX-dstY);
		return 14*dstX + 10 * (dstY-dstX);
	}


}
