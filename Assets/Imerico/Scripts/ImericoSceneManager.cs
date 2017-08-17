using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using A1050055001a;

public class ImericoSceneManager : MonoBehaviour, ICameraRendererListener {
    public GameObject ScanLine;
    public GUIText Text;
	public Camera ARCamera;

    #region MonoBehaviour

    void Start () {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
          
        // Register our callbacks for the events. 
        Manager.Instance.FinderRequestingEvent += new Manager.FinderRequestingDelegate(HandleFinderRequesting);
        Manager.Instance.FinderResultEvent += new Manager.FinderResultDelegate(HandleFinderResult);
		Manager.Instance.FinderCodeResultEvent += new Manager.FinderCodeResultDelegate(HandleCodeFinderResult);
        Manager.Instance.TrackerResultEvent += new Manager.TrackerResultDelegate(HandleTrackerResult);
        Manager.Instance.NetworkErrorEvent += new Manager.NetworkErrorDelegate(HandleNetworkError);

        ScanLine.SetActive(true);
		ARCamera.gameObject.SetActive (false);

        // We will start the recognition when the camera is ready
        CameraRenderer.Instance.Listener = this;
    }

    #endregion

	void Update ()
	{
        if (Input.GetMouseButtonDown(0))
		{
            // Back to scanning if user touches the screen
            StartScanning();
		}
	}

    #region webcam listener

	/// <summary>
	/// Called when the camera is initialized and ready.
	/// </summary>
	/// <param name="focalLengthInPixels">Focal length in pixels.</param>
	/// <param name="width">Camera Width.</param>
	/// <param name="height">Camera Height.</param>
    public void OnCameraReady(float focalLengthInPixels, int width, int height) {
        // These three parameters cannot be changed without re-starting the Manager:
        Manager.Instance.Initialize(focalLengthInPixels, width, height);

		// Let's start scanning
        StartScanning();
    }

	/// <summary>
	/// Called by the camera when a new frame is available.
	/// </summary>
	/// <param name="camera">Camera.</param>
    public void OnNewFrameAvailable(CameraRenderer camera) {
		if(!Manager.Instance.IsMarkerRecognitionBusy)
			// If we are not waiting for the result of a previous marker detection request, we perform a new one.
        	Manager.Instance.NewFrame(camera.GetFrame(), Manager.DETECT_ALL);
		else 
			// Otherwise, to avoid overloading, we only perform a code detection.
			Manager.Instance.NewFrame(camera.GetFrame(), Manager.CODE_DETECTION);

		/*
		 * Detection method aliases:
		 * Manager.Instance.Detect(camera.GetFrame()) <=> Manager.Instance.NewFrame(camera.GetFrame(), Manager.DETECT_ALL)
		 * Manager.Instance.DetectMarker(camera.GetFrame()) <=> Manager.Instance.NewFrame(camera.GetFrame(), Manager.MARKER_DETECTION)
		 * Manager.Instance.DetectCode(camera.GetFrame()) <=> Manager.Instance.NewFrame(camera.GetFrame(), Manager.CODE_DETECTION)
		 */
    }

	/// <summary>
	/// Called when the screen orientation changed.
	/// </summary>
	/// <param name="width">Screen Width.</param>
	/// <param name="height">Screen Height.</param>
	/// <param name="screenCameraRatio">Screen camera ratio.</param>
	public void OnScreenOrientationChanged(int width, int height, Vector2 screenCameraRatio) {
        // For tracking camera
		ProjectionMatrixUtil.UpdateProjectionMatrix (ARCamera, width, height, screenCameraRatio);
	}
    
    #endregion

    private void StartScanning() {
        Text.text = "Scanning...";
        ScanLine.SetActive(true);

        Manager.Instance.EnterMode(Manager.Mode.SCANNING);
    }

    #region Event Handlers

	/// <summary>
	/// Called everytime a request is sent.
	/// </summary>
    void HandleFinderRequesting() {
        Debug.Log("Fire request.");
    }

	/// <summary>
	/// Called when the result of the previous request for marker detection is available.
	/// </summary>
	/// <param name="marker">Information about the recognized marker. Null if no maker found.</param>
	/// <param name="fromLocal">If set to <c>true</c> marker was recognized from local.</param>
    void HandleFinderResult(Marker marker, bool fromLocal) {
        if (marker != null)
        {
			// Marker found!
			Text.text = string.Format("#{0}\n{1}", marker.id.ToString(), marker.name);

            // Enable tracking if available (currently only for iOS and Android)
            if(Manager.Instance.TrackingAvailable)
			    Manager.Instance.EnterMode(Manager.Mode.TRACKING);
            else
                Manager.Instance.EnterMode(Manager.Mode.IDLE);

            ScanLine.SetActive(false);
        }
    }

	/// <summary>
	/// Called when a code has been detected.
	/// </summary>
	/// <param name="symbols">List of symbols detected in the frame. Never empty.</param>
	void HandleCodeFinderResult(List<Symbol> symbols) {
		if (symbols.Count > 0) {
			Text.text = string.Format ("{0}: {1}", symbols [0].Type.ToString (), symbols [0].Data);
			Manager.Instance.EnterMode(Manager.Mode.IDLE);
			ScanLine.SetActive(false);
		}
	}

	/// <summary>
	/// Handles the tracking result.
	/// </summary>
	/// <param name="result">If set to <c>true</c> marker was tracked successfully.</param>
	/// <param name="position">Position of the camera.</param>
	/// <param name="rotation">Rotation of the camera.</param>
    void HandleTrackerResult(bool result, Vector3 position, Quaternion rotation) {
		ARCamera.gameObject.SetActive(result);
		
        if(!result) {
			// If we lost tracking, we go back to scanning.
            StartScanning();
        }
		else {
			// If the tracking is good, we setup the position of the AR camera.
			ARCamera.transform.position = position;
			ARCamera.transform.rotation = rotation;
		}
    }

	/// <summary>
	/// Handles the network error.
	/// </summary>
	/// <param name="message">Message.</param>
    void HandleNetworkError(string message) {
		Debug.LogError(string.Format("Network error: {0}", message));
    }

    #endregion
}
