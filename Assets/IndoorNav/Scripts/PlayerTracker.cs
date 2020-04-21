using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SpatialTracking;

public class PlayerTracker : MonoBehaviour
{
    [SerializeField] Transform cam;
    [SerializeField] Text camPoseText;
    TrackedPoseDriver trackedPoseDriver;
    Vector3 m_prevARPosePosition;
    bool trackingStarted = false;

    Pose GetCameraOriginPose()
    {
        if (trackedPoseDriver != null)
        {
            var localOriginPose = trackedPoseDriver.originPose;
            var parent = cam.parent;

            if (parent == null)
                return localOriginPose;

            return parent.TransformPose(localOriginPose);
        }

        return Pose.identity;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_prevARPosePosition = Vector3.zero;
        trackedPoseDriver = cam.GetComponent<TrackedPoseDriver>();
    }

    void Update()
    {
        transform.position = new Vector3(cam.position.x, transform.position.y, cam.position.z);
        transform.eulerAngles = new Vector3(0, cam.eulerAngles.y, 0);

        //_QuitOnConnectionErrors();
        
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            trackingStarted = false;                      // if tracking lost or not initialized
            camPoseText.text = "Lost tracking, wait ...";
            const int LOST_TRACKING_SLEEP_TIMEOUT = 15;
            Screen.sleepTimeout = LOST_TRACKING_SLEEP_TIMEOUT;
            return;
        }
        camPoseText.text = "tracked...";
        /*
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Vector3 currentARPosition = GetCameraOriginPose().position;
        if (!trackingStarted)
        {
            trackingStarted = true;
            m_prevARPosePosition = GetCameraOriginPose().position;
        }
        //Remember the previous position so we can apply deltas
        Vector3 deltaPosition = currentARPosition - m_prevARPosePosition;
        m_prevARPosePosition = currentARPosition;
        // The initial forward vector of the sphere must be aligned with the initial camera direction in the XZ plane.
        // We apply translation only in the XZ plane.
        this.transform.Translate(deltaPosition.x, 0.0f, deltaPosition.z);
        // Set the pose rotation to be used in the CameraFollow script
        //


        // Clear camPoseText if no error
        //if(camPoseText != null)
        //camPoseText.text = "CamPose: " + cameraTarget.transform.position;
        camPoseText.text = "" + deltaPosition;
        //m_firstPersonCamera.GetComponent<FollowTarget>().targetRot = Frame.Pose.rotation;
        */
    }
}
