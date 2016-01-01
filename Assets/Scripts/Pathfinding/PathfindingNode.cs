using UnityEngine;
using System.Collections;

public class PathfindingNode : IHeapItem<PathfindingNode> {


	public Node node;
	public int gCost;
	public int hCost;
	public PathfindingNode parent;
	int heapIndex;

	public PathfindingNode(Node _node) {
		node = _node;
		//node.pathfindingNode = this;
	}

	public int fCost {
		get {
			return gCost + hCost;
		}
	}

	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

	public int CompareTo(PathfindingNode nodeToCompare) {
		int compare = fCost.CompareTo(nodeToCompare.fCost);
		if (compare == 0) {
			compare = hCost.CompareTo(nodeToCompare.hCost);
		}
		return -compare;
	}
}
