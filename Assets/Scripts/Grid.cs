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
	Node[,] terrain;
	HashSet<BoidNode>[,] units;

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
	void scanWorld(){
		Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;
		for (int x = 0; x < gridSizeX; x ++) {
			for (int y = 0; y < gridSizeY; y ++) {
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
				bool walkable = !(Physics.CheckSphere (worldPoint, nodeRadius - ObjectSkinThickness, unwalkableMask));
				
				int movementPenalty = 0;
				if (walkable) {
					int factor = 50;
					Ray ray = new Ray (worldPoint + Vector3.up * factor, Vector3.down);
					RaycastHit hit;
					if (Physics.Raycast (ray, out hit, factor * 2, walkableMask)) {
						walkableRegionsDictionary.TryGetValue (hit.collider.gameObject.layer, out movementPenalty);
					}
				}
				terrain [x, y] = new Node (walkable, worldPoint, x, y, movementPenalty);
			}
		}
	}
	void CreateGrid() {
		terrain = new Node[gridSizeX,gridSizeY];
		scanWorld ();
		units = new  HashSet<BoidNode>[gridSizeX,gridSizeY];
		Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;
		for (int x = 0; x < gridSizeX; x ++) {
			for (int y = 0; y < gridSizeY; y ++) {
				units[x,y] = new HashSet<BoidNode>();
			}
		}
	}

	public Node[] GetTerrainNeighbours(Node node, bool radialNeigbourhood = false, float radius = 1, bool includeSameSlot = true) {
		List<Node> neighbours = new List<Node>();
		int radiusInt = -(int)Mathf.Floor (-radius);
		for (int x = -radiusInt; x <= radiusInt; x++) {
			for (int y = -radiusInt; y <= radiusInt; y++) {
				int xy = Mathf.Abs( Mathf.Abs(x)+ Mathf.Abs(y));

				if((!radialNeigbourhood && new Vector2(x,y).magnitude > radius))
					continue;

				if (!includeSameSlot && x == 0 && y == 0) 
					continue;

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					neighbours.Add(terrain[checkX,checkY]);
				}
			}
		}

		return neighbours.ToArray();
	}
	public void RegisterUnit (BoidNode boid)
	{
		Node node = NodeFromWorldPoint(boid.position);
		boid.node = node;
		units[node.gridX,node.gridY].Add(boid);
		//print ("Added Boid  at ["+ node.gridX +"," + node.gridY+"] total:"+units[node.gridX,node.gridY].Count);

	}
	public void RemoveUnit (BoidNode boid)
	{
		Node node = NodeFromWorldPoint(boid.position);
		boid.node = null;
		units[node.gridX,node.gridY].Remove(boid);

		//print ("Removed Boid  at ["+ node.gridX +"," + node.gridY+"] total:"+units[node.gridX,node.gridY].Count);
	}

	public BoidNode[] GetUnitNeighbours(BoidNode boid, bool radialNeighbourhood = false, float radius = 1) {
		List<BoidNode> neighbours = new List<BoidNode>();
		int radiusInt = (int)Mathf.Ceil (radius);
		for (int x = -radiusInt; x <= radiusInt; x++) {
			for (int y = -radiusInt; y <= radiusInt; y++) {

				if((!radialNeighbourhood && new Vector2(x,y).magnitude > radius))
					continue;

				Node node = NodeFromWorldPoint(boid.position);
				int checkX = node.gridX + x;
				int checkY = node.gridY + y;
				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					HashSet<BoidNode> neighboursXY = units[checkX,checkY];
					foreach(BoidNode neighbour in neighboursXY)
					{
						if(boid.id != neighbour.id)
							neighbours.Add(neighbour);
					}
				}
			}
		}
		
		return neighbours.ToArray();
	}


	public Node NodeFromWorldPoint(Vector3 worldPosition) {
		float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
		float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);

		int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
		return terrain[x,y];
	}
	
	void OnDrawGizmos() {
		Gizmos.DrawWireCube(transform.position,new Vector3(gridWorldSize.x,1,gridWorldSize.y));
		if (terrain != null && displayGridGizmos) {
			foreach (Node n in terrain) {
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