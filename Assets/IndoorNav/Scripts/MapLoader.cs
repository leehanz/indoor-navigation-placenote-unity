using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
/*========================================
 * Class for Loading Map
======================================== */
public class MapLoader : MonoBehaviour, PlacenoteListener {
    const string MAP_NAME = "GenericMap";
    CustomShapeManager mShapeManager;
    LibPlacenote.MapInfo mSelectedMapInfo;
    string mSelectedMapId
    {
        get
        {
            return mSelectedMapInfo != null ? mSelectedMapInfo.placeId : null;
        }
    }

    [SerializeField] NavController mNavController;
    [SerializeField] ARSession mSession;
    [SerializeField] Text mLabelText;
    [SerializeField] Button mStartBtn;
    [SerializeField] RawImage mLocalizationThumbnail;

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
        Debug.Assert(mLabelText != null, "debugText is missing.");
        Debug.Assert(mStartBtn != null, "startBtn is missing.");
        Debug.Assert(mNavController != null, "navController is missing.");

        mNavController.OnStartedEvent.AddListener(ActivateStartBtn);
        mLabelText.text = "Initializing...";

        Input.location.Start();
        Application.targetFrameRate = 60;
        StartCoroutine(WaitForARSessionThenDo(()=>
        {
            // Activate placenote
            LibPlacenote.Instance.RegisterListener(this); // Register listener for onStatusChange OnPose
            FeaturesVisualizer.EnablePointcloud(); // Optional - to see the point features
            // AR Session has started tracking here. Now start the session
            ARSessionReady = true;
            mLabelText.text = "Ready to Start";
            FindMap();
        }));

        // Localization thumbnail handler.
        mLocalizationThumbnail.gameObject.SetActive(false);

        // Set up the localization thumbnail texture event.
        LocalizationThumbnailSelector.Instance.TextureEvent += (thumbnailTexture) =>
        {
            if (mLocalizationThumbnail == null)
            {
                return;
            }

            RectTransform rectTransform = mLocalizationThumbnail.rectTransform;
            if (thumbnailTexture.width != (int)rectTransform.rect.width)
            {
                rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, thumbnailTexture.width * 2);
                rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, thumbnailTexture.height * 2);
                rectTransform.ForceUpdateRectTransforms();
            }
            mLocalizationThumbnail.texture = thumbnailTexture;
        };
        
    }

    void OnDestroy()
    {
        ConfigureSession();
    }

    void ConfigureSession()
    {
        mSession.Reset();
        /*
#if !UNITY_EDITOR
		ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration ();
		config.planeDetection = UnityARPlaneDetection.None;
		config.alignment = UnityARAlignment.UnityARAlignmentGravity;
		config.getPointCloudData = true;
		config.enableLightEstimation = true;
		mSession.RunWithConfig (config);
#endif
*/
    }

    void ActivateStartBtn()
    {
        mStartBtn.gameObject.SetActive(true);
    }

    public void OnStartNavigationClicked()
    {
        if (mNavController.navigationState == NavController.State.Completed)
        {
            mNavController.DrawNavigation();
            mStartBtn.gameObject.SetActive(false);
        }
    }

    #region Load Map Methods
    void FindMap() {
        //get metadata
        LibPlacenote.Instance.SearchMaps(MAP_NAME, (LibPlacenote.MapInfo[] obj) =>
        {
            foreach (LibPlacenote.MapInfo map in obj) {
                if (map.metadata.name == MAP_NAME) {
                    mSelectedMapInfo = map;
                    Debug.Log("FOUND MAP: " + mSelectedMapInfo.placeId);
                    mLabelText.text = "Downloading map: " + MAP_NAME;
                    LoadMap();
                    return;
                }
            }
        });
    }

    void LoadMap() {
        ConfigureSession();
        LibPlacenote.Instance.LoadMap(mSelectedMapInfo.placeId,
            (completed, faulted, percentage) => {
                if (completed) {
                    // Disable pointcloud
                    FeaturesVisualizer.DisablePointcloud();
                    LibPlacenote.Instance.StartSession();
                    mLocalizationThumbnail.gameObject.SetActive(true);
                    mLabelText.text = "Loaded Map. Trying to localize...";
                } else if (faulted) {
                    mLabelText.text = "Failed to load ID: " + mSelectedMapId;
                } else {
                    if (!float.IsNaN(percentage))
                    {
                        mLabelText.text = "downloading map..." + (percentage * 100).ToString("N0") + "%";
                    }
                }
            }
        );
    }
    #endregion

    #region Placenote Event
    public void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
    {
        Debug.Log("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());

        if (currStatus == LibPlacenote.MappingStatus.LOST && prevStatus == LibPlacenote.MappingStatus.WAITING)
        {
            Debug.Log("Searching for position lock");
            mLabelText.text = "Point your phone at the area shown in the thumbnail";
        }
        else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST)
        {
            Debug.Log("Localized: " + mSelectedMapInfo.metadata.name);
        }
    }

    public void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose) { }

    public void OnLocalized()
    {
        mLocalizationThumbnail.gameObject.SetActive(false);
        mShapeManager.LoadShapesJSON(mSelectedMapInfo.metadata.userdata,
                () => {
                    if (mNavController != null)
                    {
                        mLabelText.text = "Get back to your starting point.";
                        mNavController.InitNavigationPath();
                    }
                });
        
    }
    #endregion
}
