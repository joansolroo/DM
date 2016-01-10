using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


public class Flocker {
	
	public float cohesionRadius = 1f;
	public float maxCohesionForce = 0.1f;
	
	public float separationRadius = 0.5f;
	public float maxSeparationForce = 0.25f;
	
	float AlignmentRadius = 0.5f;
	float maxAlignmentForce = 0.1f;
	
	public static Vector3 ComputeCohesionForce(BoidNode boid, BoidNode[] neighbourUnits, float maxRadius, float maxForce){
		Vector3 force = new Vector3();
		Vector3 centroid = new Vector3();
		int neighborCount = 0;
		foreach(BoidNode b in neighbourUnits){
			if (b == boid)
				continue;
			float distance = boid.DistanceBetweenBorders(b);
			if (b != boid &&  distance < maxRadius) {
				centroid += b.position * boid.mass;
				neighborCount++;
			}
		}
		
		if (neighborCount != 0) {
			centroid = centroid /neighborCount;	
			float distance = boid.DistanceFromBorder(centroid);
			force = (boid.position-centroid)*Mathf.Max(1,distance/maxRadius);
			force.Normalize();
			force *= maxRadius;
			
		}
		return force;
	}
	
	public static Vector3 ComputeSeparationForce(BoidNode boid, BoidNode[] neighbourUnits, float maxRadius, float maxForce){
		Vector3 force = new Vector3();
		int neighborCount = 0;
		
		foreach(BoidNode b in neighbourUnits){
			if (b == boid)
				continue;
			float distance = boid.DistanceBetweenBorders(b);
			if (b != boid &&  distance < maxRadius) {
				force +=  (boid.position-b.position)*(1-distance/maxRadius) * boid.mass;
				neighborCount++;
			}
		}
		
		if (neighborCount != 0) {
			force = force/(neighborCount);	
			force.Normalize();
			force *= maxForce;
		}
		// Assume all the neigbours will make an equivalent effort
		return force;
	}
	
	public static Vector3 ComputeAlignmentForce(BoidNode boid, BoidNode[] neighbourUnits, float maxRadius, float maxForce){
		Vector3 force = new Vector3();
		Vector3 velocity = new Vector3();
		int neighborCount = 0;
		
		foreach(BoidNode b in neighbourUnits){
			if (b == boid)
				continue;
			float distance = boid.DistanceBetweenBorders(b);
			if (b != boid &&  distance < maxRadius) {
				velocity += b.velocity*(1-distance/maxRadius) * boid.mass;
				neighborCount++;
			}
		}
		
		if (neighborCount != 0) {
			velocity = velocity/neighborCount;
			force.Normalize();
			force *= maxForce;
		}
		
		return velocity;
	}
	public Vector3 ComputeInfluence(BoidNode boid, BoidNode[] neighbourUnits, float separationFactor, float cohesionFactor, float alignmentFactor){
		Vector3 separation = ComputeSeparationForce (boid, neighbourUnits,separationRadius,maxSeparationForce);
		Vector3 cohesion = ComputeCohesionForce (boid, neighbourUnits, cohesionRadius, maxCohesionForce);
		Vector3 alignment = ComputeAlignmentForce (boid, neighbourUnits, AlignmentRadius, maxAlignmentForce);
		Vector3 unitForce = separation * separationFactor + cohesion * cohesionFactor + alignment * alignmentFactor;
		return unitForce;
	}
}