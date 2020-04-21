using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*========================================
 * Class for Controlling Minimap
======================================== */
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(LineRenderer))]
public class MinimapController : MonoBehaviour
{
    Camera cam;
	LineRenderer lr;
	CurvedLineRenderer clr;
    [SerializeField] Transform target;

	// The distance in the x-z plane to the target
	[SerializeField] float distance = 0.8f;
	// the height we want the camera to be above the target
	[SerializeField] float height = 1.5f;
	float heightDamping = 2.0f;
	float rotationDamping = 3.0f;

	void Start()
    {
        cam = GetComponent<Camera>();
		lr = GetComponent<LineRenderer>();
		clr = GetComponent<CurvedLineRenderer>();

	}

	void LateUpdate()
	{
		// Early out if we don't have a target
		if (!target) return;

		// Calculate the current rotation angles
		float wantedRotationAngle = target.eulerAngles.y;
		float wantedHeight = target.position.y + height;

		float currentRotationAngle = transform.eulerAngles.y;
		float currentHeight = transform.position.y;

		// Damp the rotation around the y-axis
		currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);

		// Damp the height
		currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

		// Convert the angle into a rotation
		var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

		// Set the position of the camera on the x-z plane to:
		// distance meters behind the target
		transform.position = target.position;
		transform.position -= currentRotation * Vector3.forward * distance;

		// Set the height of the camera
		transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);

		// Always look at the target
		transform.LookAt(target);
	}

	public void DrawPath(Transform[] pts)
	{
		List<Transform> ptList = new List<Transform>();
		ptList.Add(target);
		ptList.AddRange(pts);

		lr.positionCount = 0;
		clr.UpdatePoints(ptList.ToArray());
	}
}
