using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class CurvedLineRenderer : MonoBehaviour
{
	//PUBLIC
	public float lineSegmentSize = 0.15f;
	public float lineWidth = 0.6f;

	//PRIVATE
	private Transform[] linePoints = new Transform[0];
	private Vector3[] linePositions = new Vector3[0];
	private Vector3[] linePositionsOld = new Vector3[0];

	public void UpdatePoints(Transform[] pts)
	{
		linePoints = pts;
		GetPoints();
		SetPointsToLine();
	}

	void GetPoints()
	{
		if (linePoints.Length == 0) return;
		//find curved points in children
		//linePoints = this.GetComponentsInChildren<CurvedLinePoint>();

		//add positions
		linePositions = new Vector3[linePoints.Length];
		for( int i = 0; i < linePoints.Length; i++ )
		{
			linePositions[i] = linePoints[i].position;
		}
	}

	void SetPointsToLine()
	{
		if (linePoints.Length == 0) return;
		//create old positions if they dont match
		if ( linePositionsOld.Length != linePositions.Length )
		{
			linePositionsOld = new Vector3[linePositions.Length];
		}

		//check if line points have moved
		bool moved = false;
		for( int i = 0; i < linePositions.Length; i++ )
		{
			//compare
			if( linePositions[i] != linePositionsOld[i] )
			{
				moved = true;
			}
		}

		//update if moved
		if( moved == true )
		{
			LineRenderer line = this.GetComponent<LineRenderer>();

			//get smoothed values
			Vector3[] smoothedPoints = LineSmoother.SmoothLine( linePositions, lineSegmentSize );

			//set line settings
			line.positionCount = smoothedPoints.Length;
			line.SetPositions( smoothedPoints );
			line.startWidth = line.endWidth = lineWidth;
		}
	}
}
