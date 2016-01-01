using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid : MonoBehaviour {

	public static Grid instance;

	public bool displayGridGizmos;
	
	public Vector2 gridWorldSize;
	public float nodeRadius;
	float nodeDiameter;
	int gridSizeX, gridSizeY;
	Node[,] grid;

	float ObjectSkinThickness = 0.001f ;

	public LayerMask unwalkableMask;
	LayerMask walkableMask;
	public TerrainType[] WalkableRegions;
	Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
	
	public Grid(){
		if (instance == null)
			instance = this;
		else if (instance != this)
			//Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
			Destroy(gameObject); 
	}
	void Awake() {

		nodeDiameter = nodeRadius*2;
		gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
		gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);

		foreach (TerrainType type in WalkableRegions) {
			walkableMask |= type.layerMask.value;
			walkableRegionsDictionary.Add((int)Mathf.Log(type.layerMask.value,2), type.terrainPenalty);
		}
		CreateGrid();
	}

	public int MaxSize {
		get {
			return gridSizeX * gridSizeY;
		}
	}

	void CreateGrid() {
		grid = new Node[gridSizeX,gridSizeY];
		Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;
		for (int x = 0; x < gridSizeX; x ++) {
			for (int y = 0; y < gridSizeY; y ++) {
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
				bool walkable = !(Physics.CheckSphere(worldPoint,nodeRadius-ObjectSkinThickness,unwalkableMask));

				int movementPenalty = 0;
				if(walkable){
					int factor = 50;
					Ray ray = new Ray(worldPoint+ Vector3.up * factor, Vector3.down);
					RaycastHit hit;
					if(Physics.Raycast(ray, out hit, factor * 2, walkableMask)){
						walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
					}
				}
				grid[x,y] = new Node(walkable,worldPoint, x, y, movementPenalty);
			}
		}
	}

	public List<Node> GetNeighbours(Node node, bool includeDiagonals = false) {
		List<Node> neighbours = new List<Node>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				int xy = Mathf.Abs( Mathf.Abs(x)+ Mathf.Abs(y));
				if (x == 0 && y == 0 || (!includeDiagonals && xy == 2)) 
					continue;

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					neighbours.Add(grid[checkX,checkY]);
				}
			}
		}

		return neighbours;
	}
	

	public Node NodeFromWorldPoint(Vector3 worldPosition) {
		float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
		float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);

		int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
		return grid[x,y];
	}
	
	void OnDrawGizmos() {
		Gizmos.DrawWireCube(transform.position,new Vector3(gridWorldSize.x,1,gridWorldSize.y));
		if (grid != null && displayGridGizmos) {
			foreach (Node n in grid) {
				if(n.walkable){

					Gizmos.color = Color.white;
					Gizmos.DrawWireCube(n.worldPosition, Vector3.one * (nodeDiameter));
				}
				else{
					Gizmos.color = Color.red;
					Gizmos.DrawWireSphere(n.worldPosition, nodeRadius-ObjectSkinThickness);
					Gizmos.color = Color.blue;
					Gizmos.DrawWireSphere(n.worldPosition, nodeRadius+ObjectSkinThickness);
				}
			}
		}
	}

	[System.Serializable]
	public struct TerrainType {
		public LayerMask layerMask;
		public int terrainPenalty;
	}
}