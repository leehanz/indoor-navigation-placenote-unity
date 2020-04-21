using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

/*========================================
 * Main Class for Managing Markers
======================================== */

public class CustomShapeManager : MonoBehaviour {
	enum ShapeType { Start, Waypoint, Destination }

    [SerializeField]
	GameObject START_PREFAB;
	[SerializeField]
	GameObject WAYPOINT_PREFAB;  // for creator
	[SerializeField]
	GameObject WAYPOINT_ARROW_PREFAB; // for loader
	[SerializeField]
	GameObject DESTINATION_PREFAB;

	[SerializeField]
	Transform logContainer;
	[SerializeField]
	Text logPrefab;

	[HideInInspector]
    public List<ShapeInfo> shapeInfoList = new List<ShapeInfo>();
	[HideInInspector]
    public List<GameObject> shapeObjList = new List<GameObject>();

	private GameObject lastShape;

	private bool shapesLoaded = false;
	//-------------------------------------------------
	// All shape management functions (add shapes, save shapes to metadata etc.
	//-------------------------------------------------

	private void Log(string str)
	{
		var log = Instantiate(logPrefab);
		log.text = str;
		log.transform.parent = logContainer;
	}

    public void AddShape(Vector3 shapePosition, Quaternion shapeRotation, bool isStart, bool isDestination)
    {
		int typeIndex = (int)ShapeType.Waypoint;//waypoint 
		if (isDestination)
            typeIndex = (int)ShapeType.Destination;//destination 
		else if (isStart)
            typeIndex = (int)ShapeType.Start;//start

		ShapeInfo shapeInfo = new ShapeInfo();
        shapeInfo.px = shapePosition.x;
        shapeInfo.py = shapePosition.y;
        shapeInfo.pz = shapePosition.z;
        shapeInfo.qx = shapeRotation.x;
        shapeInfo.qy = shapeRotation.y;
        shapeInfo.qz = shapeRotation.z;
        shapeInfo.qw = shapeRotation.w;
		shapeInfo.shapeType = typeIndex.GetHashCode();
        shapeInfoList.Add(shapeInfo);

        GameObject shape = ShapeFromInfo(shapeInfo);
        shapeObjList.Add(shape);
		Log(string.Format("drop shape({0}) type={1}", shapeObjList.Count-1, shapeInfo.shapeType));
	}

	public void AddDestinationShape ()
    {
		//change last waypoint to destination
		ShapeInfo lastInfo = shapeInfoList [shapeInfoList.Count - 1];
		lastInfo.shapeType = ((int)ShapeType.Destination).GetHashCode ();
		GameObject shape = ShapeFromInfo(lastInfo);
		shape.GetComponent<Node>().Activate(true);
		//destroy last shape
		Destroy (shapeObjList [shapeObjList.Count - 1]);
		//add new shape
		shapeObjList.Add (shape);
		Log(string.Format("set destination({0}) type={1}", shapeObjList.Count - 1, lastInfo.shapeType));
	}

    public GameObject ShapeFromInfo(ShapeInfo info)
    {
		GameObject shape = null;
		Vector3 position = new Vector3 (info.px, info.py, info.pz);


		if (info.shapeType == (int)ShapeType.Waypoint)
		{
			//if loading map, change waypoint to arrow
			if (SceneManager.GetActiveScene().name == "MapLoader")
			{
				shape = Instantiate(WAYPOINT_ARROW_PREFAB);
			}
			else
			{
				shape = Instantiate(WAYPOINT_PREFAB);
			}
		}
		else if (info.shapeType == (int)ShapeType.Start)
		{
			shape = Instantiate(START_PREFAB);
		}
		else
		{
			//Destination
			shape = Instantiate(DESTINATION_PREFAB);
		}

		var node = shape.GetComponent<Node>();
		if (node != null)
        {
			node.pos = position;
            Debug.Log(position);
		}

		shape.tag = "waypoint";
		shape.transform.position = position;
		shape.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
		//shape.transform.localScale = new Vector3(.3f, .3f, .3f);
		Log(string.Format("add shape obj: type={0}", info.shapeType));
		return shape;
    }

    public void ClearShapes()
    {
        foreach (var obj in shapeObjList)
        {
            Destroy(obj);
        }
        shapeObjList.Clear();
        shapeInfoList.Clear();
    }


    public JObject Shapes2JSON()
    {
        ShapeList shapeList = new ShapeList();
        shapeList.shapes = new ShapeInfo[shapeInfoList.Count];
        for (int i = 0; i < shapeInfoList.Count; i++)
        {
            shapeList.shapes[i] = shapeInfoList[i];
        }

        return JObject.FromObject(shapeList);
    }

    public void LoadShapesJSON(JToken mapMetadata, Action callback)
    {
		if (!shapesLoaded) {
			shapesLoaded = true;
            Log("LOADING SHAPES>>>");
			if (mapMetadata is JObject && mapMetadata ["shapeList"] is JObject) {
				ShapeList shapeList = mapMetadata ["shapeList"].ToObject<ShapeList> ();
				if (shapeList.shapes == null) {
					Debug.Log ("no shapes dropped");
					return;
				}
				Log(string.Format("found {0} shapes...", shapeList.shapes.Length));
				foreach (var shapeInfo in shapeList.shapes) {
					shapeInfoList.Add (shapeInfo);
					GameObject shape = ShapeFromInfo (shapeInfo);
					shapeObjList.Add (shape);
				}
			}

			callback?.Invoke();
		}
	}
}
