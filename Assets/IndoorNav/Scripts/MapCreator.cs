using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Newtonsoft.Json.Linq;
/*========================================
 * Class for Creating Map
======================================== */
[RequireComponent(typeof(CustomShapeManager))]
public class MapCreator : MonoBehaviour, PlacenoteListener {
    const string MAP_NAME = "GenericMap";
    LibPlacenote.MapMetadataSettable mCurrMapDetails;
    CustomShapeManager mShapeManager;

    [SerializeField] ARSession mSession;
    [SerializeField] Text mLabelText;
    [SerializeField] Button mBackBtn;

    bool hasSetStartingPoint = false;
    bool shouldRecordWaypoints = false;
    bool shouldSaveMap = true;
    bool ARSessionReady = false;

    IEnumerator WaitForARSessionThenDo(Action action)
    {
        while (ARSession.state != ARSessionState.SessionTracking || !LibPlacenote.Instance.Initialized())
        {
            yield return null;
        }

        action.Invoke();
    }

    void Start()
    { 
        mShapeManager = GetComponent<CustomShapeManager>();
        Debug.Assert(mShapeManager != null, "shapeManager is missing.");
        Debug.Assert(mLabelText != null, "mLabelText is missing.");
        Debug.Assert(mBackBtn != null, "backBtn is missing.");
        mLabelText.text = "Initializing...";

        Input.location.Start();
        Application.targetFrameRate = 60;
        StartCoroutine(WaitForARSessionThenDo(() =>
        {
            // activate placenote SDK
            LibPlacenote.Instance.RegisterListener(this); //Register listener for onStatusChange, OnPose, OnLocalized
            FeaturesVisualizer.EnablePointcloud(); //Optional - to see the point features
            ARSessionReady = true;
            mLabelText.text = "Ready to Start";
        }));
    }

    void OnDestroy()
    {
        ConfigureSession();
    }

    // Update is called once per frame
    void Update()
    {
        if (!ARSessionReady) return;

        if (shouldRecordWaypoints)
        {
            Transform player = Camera.main.transform;
            //create waypoints if there are none around
            Collider[] hitColliders = Physics.OverlapSphere(player.position, 1f);
            int i = 0;
            while (i < hitColliders.Length)
            {
                if (hitColliders[i].CompareTag("waypoint"))
                    return;
                i++;
            }
            Vector3 pos = player.position;
            //Debug.Log(player.position);
            pos.y = -.5f;

            if (!hasSetStartingPoint)
            {
                //set start point
                hasSetStartingPoint = true;
                mShapeManager.AddShape(pos, Quaternion.Euler(Vector3.zero), true, false);
            }
            else
            {
                //set waypoints
                mShapeManager.AddShape(pos, Quaternion.Euler(Vector3.zero), false, false);
            }
        
        }
    }

    void ConfigureSession()
    {
        mSession.Reset();
        /*
#if !UNITY_EDITOR
		ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration ();

		if (UnityARSessionNativeInterface.IsARKit_1_5_Supported ()) {
			config.planeDetection = UnityARPlaneDetection.HorizontalAndVertical;
		} else {
			config.planeDetection = UnityARPlaneDetection.Horizontal;
		}

		config.alignment = UnityARAlignment.UnityARAlignmentGravity;
		config.getPointCloudData = true;
		config.enableLightEstimation = true;
		mSession.RunWithConfig (config);
#endif
*/
    }

    #region Create Map Methods
    public void CreateDestination()
    {
        mShapeManager.AddDestinationShape();
    }

    public void OnStartNewMapClick()
    {
        //check SDK
        if (!LibPlacenote.Instance.Initialized()) return;

        ConfigureSession();

        //start new session
        LibPlacenote.Instance.StartSession();

        //start drop waypoint
        shouldRecordWaypoints = true;
        mLabelText.text = "Walk around and set the destination";
    }

    public void OnSaveMapClick()
    {
        //check SDK
        if (!LibPlacenote.Instance.Initialized()) return;

        mLabelText.text = "Searching Existed Map...";
        // Overwrite map if it exists.
        LibPlacenote.Instance.SearchMaps(MAP_NAME, (LibPlacenote.MapInfo[] obj) =>
        {
            bool foundMap = false;
            foreach (LibPlacenote.MapInfo map in obj)
            {
                if (map.metadata.name == MAP_NAME)
                {
                    foundMap = true;
                    mLabelText.text = "Deleting Existed Map...";
                    LibPlacenote.Instance.DeleteMap(map.placeId, (deleted, errMsg) =>
                    {
                        if (deleted)
                        {
                            Debug.Log("Deleted ID: " + map.placeId);
                            SaveCurrentMap();
                        }
                        else
                        {
                            Debug.Log("Failed to delete ID: " + map.placeId);
                        }
                    });
                }
            }

            if (!foundMap)
            {
                SaveCurrentMap();
            }
        });
        shouldRecordWaypoints = false;
    }

    void SaveCurrentMap()
    {
        //check SDK
        if (!LibPlacenote.Instance.Initialized()) return;

        if (shouldSaveMap)
        {
            shouldSaveMap = false;

            bool useLocation = Input.location.status == LocationServiceStatus.Running;
            LocationInfo locationInfo = Input.location.lastData;

            Debug.Log("Saving...");
            mLabelText.text = "Uploading new map...";
            LibPlacenote.Instance.SaveMap((mapId) =>
            {
                    LibPlacenote.Instance.StopSession();

                    LibPlacenote.MapMetadataSettable metadata = new LibPlacenote.MapMetadataSettable();
                    metadata.name = MAP_NAME;
                    
                    JObject userdata = new JObject();
                    metadata.userdata = userdata;

                    JObject shapeList = mShapeManager.Shapes2JSON();

                    userdata["shapeList"] = shapeList;

                    if (useLocation) {
                        metadata.location = new LibPlacenote.MapLocation();
                        metadata.location.latitude = locationInfo.latitude;
                        metadata.location.longitude = locationInfo.longitude;
                        metadata.location.altitude = locationInfo.altitude;
                    }
                    LibPlacenote.Instance.SetMetadata(mapId, metadata);
                    mCurrMapDetails = metadata;
                    mLabelText.text = "Saved map ( " + metadata.name+")";
                },
                (completed, faulted, percentage) =>
                {
                    if (completed) {
                        Debug.Log("Upload Complete:" + mCurrMapDetails.name);
                        mLabelText.text = "Upload completed.";
                        mBackBtn.gameObject.SetActive(true);
                    } else if (faulted) {
                        Debug.Log("Upload of Map Named: " + mCurrMapDetails.name + "faulted");
                    } else {
                        if (!float.IsNaN(percentage))
                        {
                            Debug.Log("Uploading Map Named: " + mCurrMapDetails.name + "(" + percentage.ToString("F2") + "/1.0)");
                            mLabelText.text = "uploading..." + (percentage * 100).ToString("N0") + "%";
                        }
                    }
                }
            );
        }
    }
    #endregion

    #region Placenote Event
    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
    {
        Debug.Log("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());
        if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {
            Debug.Log("Localized");
        } else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
            Debug.Log("Mapping");
        } else if (currStatus == LibPlacenote.MappingStatus.LOST) {
            Debug.Log("Searching for position lock");
        } else if (currStatus == LibPlacenote.MappingStatus.WAITING) {
            if (mShapeManager.shapeObjList.Count != 0) {
                mShapeManager.ClearShapes();
            }
        }
    }

    public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose) { }

    public void OnLocalized() { }
    #endregion
}
