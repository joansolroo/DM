using UnityEngine;
using System.Collections;

/* This class provides the strategy for a given unit
 */
[RequireComponent (typeof (Unit))]
public class UnitAI : MonoBehaviour {

	Unit unit;
	bool chasing;

	void Awake() {
		unit = GetComponent<Unit>();
	}
	
	// Update is called once per frame
	void Update () {
		if (!chasing) {
			chasing = true;
			unit.chase (unit.targetBoid);
		}
	}
}
