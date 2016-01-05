﻿using UnityEngine;
using System.Collections;
using UnityEditor;

public abstract class Boid : MonoBehaviour {

	public float radius = 0.5f;
	public bool ShowGizmos = false;
	protected BoidNode boid;

	protected abstract void Initialize ();

	public void OnDrawGizmos() {
		if (!ShowGizmos) {
			return;
		}
		if (boid == null)
			Initialize ();
		//Visibility
		Handles.color = Color.red;
		Handles.DrawWireDisc(transform.position , Vector3.up, boid.sight);

		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere (transform.position, boid.hardRadius);
		Handles.color = Color.white;
		Handles.DrawWireDisc(transform.position , Vector3.up, boid.softRadius);
		//Handles.color = Color.white;
		//Handles.DrawWireDisc(transform.position , Vector3.up, boid.visibilityRadius);


		Gizmos.color = Color.blue;
		Gizmos.DrawLine (transform.position, transform.position + boid.velocity);

		Gizmos.color = Color.red;
		Gizmos.DrawLine (transform.position, transform.position + boid.steering);

		Gizmos.color = Color.green;
		Gizmos.DrawLine (transform.position, transform.position + boid.aheadVector);

	}
}
