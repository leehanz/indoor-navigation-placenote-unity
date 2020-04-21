using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

 // Classes to hold shape information

[System.Serializable]
public class ShapeInfo
{
    public float px;
    public float py;
    public float pz;
    public float qx;
    public float qy;
    public float qz;
    public float qw;
    public int shapeType;
    public int colorType;
}


[System.Serializable]
public class ShapeList
{
    public ShapeInfo[] shapes;
}



 // Main Class for Managing Markers

public class ShapeManager : MonoBehaviour,PlacenoteListener {

    public List<ShapeInfo> shapeInfoList = new List<ShapeInfo>();
    public List<GameObject> shapeObjList = new List<GameObject>();
    public Material mShapeMaterial;
    private Color[] colorTypeOptions = {Color.cyan, Color.red, Color.yellow};

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            var mousePosition = Input.mousePosition;
            touchPosition = new Vector2(mousePosition.x, mousePosition.y);
            return true;
        }
#else
        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }
#endif

        touchPosition = default;
        return false;
    }
    [SerializeField]
    ARRaycastManager m_RaycastManager;
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    Vector3 hitPosition;
    Quaternion hitRotation;



    // Use this for initialization
    void Start () {
        Debug.Assert(m_RaycastManager!=null, "ARRaycastManager is null");
	}

    // Update function checks for hittest

    void Update()
    {
        //touch UI
        if (EventSystem.current.currentSelectedGameObject != null)
            return;

        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;


        if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.FeaturePoint))
        {
            // Raycast hits are sorted by distance, so the first one
            // will be the closest hit.
            var hitPose = s_Hits[0].pose;

            Debug.Log("Got hit!");

            // add shape
            AddShape(hitPosition, hitRotation);
        }
    }

	public void OnSimulatorDropShape()
	{
		Vector3 dropPosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
		Quaternion dropRotation = Camera.main.transform.rotation;

		AddShape(dropPosition, dropRotation);

	}


    // All shape management functions (add shapes, save shapes to metadata etc.

    public void AddShape(Vector3 shapePosition, Quaternion shapeRotation)
    {
        System.Random rnd = new System.Random();
        PrimitiveType type = (PrimitiveType)rnd.Next(0, 4);

        int colorType =  rnd.Next(0, 3);

        ShapeInfo shapeInfo = new ShapeInfo();
        shapeInfo.px = shapePosition.x;
        shapeInfo.py = shapePosition.y;
        shapeInfo.pz = shapePosition.z;
        shapeInfo.qx = shapeRotation.x;
        shapeInfo.qy = shapeRotation.y;
        shapeInfo.qz = shapeRotation.z;
        shapeInfo.qw = shapeRotation.w;
        shapeInfo.shapeType = type.GetHashCode();
        shapeInfo.colorType = colorType;
        shapeInfoList.Add(shapeInfo);

        GameObject shape = ShapeFromInfo(shapeInfo);
        shapeObjList.Add(shape);
    }


    public GameObject ShapeFromInfo(ShapeInfo info)
    {
        GameObject shape = GameObject.CreatePrimitive((PrimitiveType)info.shapeType);
        shape.transform.position = new Vector3(info.px, info.py, info.pz);
        shape.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
        shape.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        shape.GetComponent<MeshRenderer>().material = mShapeMaterial;
        shape.GetComponent<MeshRenderer>().material.color = colorTypeOptions[info.colorType];
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

    public void LoadShapesJSON(JToken mapMetadata)
    {
        ClearShapes();
        if (mapMetadata is JObject && mapMetadata["shapeList"] is JObject)
        {
            ShapeList shapeList = mapMetadata["shapeList"].ToObject<ShapeList>();
            if (shapeList.shapes == null)
            {
                Debug.Log("no shapes dropped");
                return;
            }

            foreach (var shapeInfo in shapeList.shapes)
            {
                shapeInfoList.Add(shapeInfo);
                GameObject shape = ShapeFromInfo(shapeInfo);
                shapeObjList.Add(shape);
            }
        }
    }

    public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose)
    {
        Debug.Log("Shape OnPose");
        Matrix4x4 camParentPose = outputPose * arkitPose.inverse;
        
        hitPosition = PNUtility.MatrixOps.GetPosition(camParentPose);
        hitRotation = PNUtility.MatrixOps.GetRotation(camParentPose);
    }

    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
    {
        //throw new System.NotImplementedException();
    }

    public void OnLocalized()
    {
        //throw new System.NotImplementedException();
    }
}
