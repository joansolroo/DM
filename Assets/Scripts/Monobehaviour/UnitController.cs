using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Unit))]
public class UnitController : MonoBehaviour {
	
	Unit unit;
	Vector3 cursorPosition;

	void Awake() {
		unit = GetComponent<Unit>();
	}
	
	void Update () 
	{
		if(Input.GetMouseButtonDown(0)){
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if(Physics.Raycast(ray, out hit))
			{
				Debug.Log(hit.collider.name);
				cursorPosition = hit.point;
				unit.MoveTo(cursorPosition);
			}
		}
	}
}
