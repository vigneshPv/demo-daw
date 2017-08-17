using UnityEngine;
using System.Collections;
using A1050055001a;

public interface ICameraRendererListener
{
	void OnCameraReady(float focalLengthInPixels, int width, int height);
	void OnNewFrameAvailable(CameraRenderer instance);
	void OnScreenOrientationChanged(int width, int height, Vector2 screenCameraRatio);
}

/// <summary>
/// Camera renderer uses a WebCamTexture in order to access the live video stream and render it on
/// the background
/// This class has been left accessible in order to let developers maintain control over the camera
/// rendering on screen
/// </summary>
/// <remarks>
/// By default, game object related to the live video stream rendering are associated to the 8th
/// layer 'LiveVideoStream'
/// </remarks>
public class CameraRenderer : MonoBehaviour
{
    public static CameraRenderer Instance
    {
        get;
        private set;
    }
	
	/// <summary>
	/// The orthographic camera used to render the live video stream,
    /// it should be set in the Unity Editor interface
    /// </summary>
    public Camera CameraSource;

    /// <summary>
    /// The focal length in pixels.
    /// </summary>
    public float FocalLengthInPixels = 551.3f;

    /// <summary>
    /// The requested width resolution.
    /// This does not ensure you will have the requested resolution, 
    /// it depends of the available streaming resolution of the camera.
    /// </summary>
    public int RequestedWidth = 640;

    /// <summary>
    /// The requested height resolution.
    /// This does not ensure you will have the requested resolution, 
    /// it depends of the available streaming resolution of the camera.
    /// </summary>
    public int RequestedHeight = 480;

	/// <summary>
	/// The Camera Renderer listener.
	/// </summary>
	public ICameraRendererListener Listener;
	
	/// <summary>
    /// The texture of the live video stream
    /// </summary>
    private WebCamTexture m_webCamTexture = null;

    /// <summary>
    /// Keeps track if the live video stream has been correctly initialized
    /// </summary>
    private bool m_initialized = false;
    
    /// <summary>
    /// Frame buffer
    /// </summary>
    /// <remarks>
    /// Buffer due to vertical inversion of live video stream
    /// </remarks>
    private Color32[] m_frameBuffer = null;

    /// <summary>
    /// Track the current screen orientation.
    /// </summary>
	private ScreenOrientation m_screenOrientation = ScreenOrientation.Unknown;
    private Vector2 m_currentScreenSize = Vector2.zero;

	/// <summary>
	/// Provide access to the live video stream texture data
	/// </summary>
	/// <returns>
	/// The pixel array of the texture
	/// </returns>
	public Color32[] GetFrame()
	{
		m_webCamTexture.GetPixels32(m_frameBuffer);
		
		return m_frameBuffer;
	}
	
	/// <summary>
	/// Provide access to the live video stream texture data.
	/// </summary>
	/// <param name="buffer">Pre-initialized Buffer.</param>
	public void GetFrame(Color32[] buffer)
	{
		m_webCamTexture.GetPixels32(buffer);
	}
	
	/// <summary>
	/// Gets a value indicating whether this <see cref="CameraRenderer"/> is initialized.
	/// </summary>
	/// <value><c>true</c> if initialized; otherwise, <c>false</c>.</value>
	public bool Initialized
	{
		get
		{
			return m_initialized;
		}
	}
	
	/// <summary>
	/// Gets the width of the live video stream texture
	/// </summary>
	/// <value>
	/// The width of the live video stream texture
	/// </value>
	public int Width
	{
		get { return m_webCamTexture.width; }
	}
	
	/// <summary>
	/// Gets the height of the live video stream texture
	/// </summary>
	/// <value>
	/// The height of the live video stream texture
	/// </value>
	public int Height
	{
		get { return m_webCamTexture.height; }
	}

	/// <summary>
	/// Gets the on screen camera ratio.
	/// </summary>
	/// <value>The on screen camera ratio.</value>
	public Vector2 OnScreenCameraRatio
	{
		get;
		private set;
	}
	
	#region MonoBehaviour specific
	
	void Awake()
	{
		Instance = this;
	}
	
	/// <summary>
	/// Monobehaviour Start method.
	/// Used here to initialize the live video stream camera texture and the quad on which it will
    /// be rendered
    /// </summary>
    /// <remarks>
    /// See Unity documentation for more information on this method
    /// </remarks>
    void Start()
    {
		if(!Application.HasUserAuthorization(UserAuthorization.WebCam))
		{
			Debug.LogError("Camera not authorized.");
			return;
		}

        //Create the webcam texture
        m_webCamTexture = new WebCamTexture();

        if (m_webCamTexture)
        {
            m_webCamTexture.requestedWidth = RequestedWidth;
            m_webCamTexture.requestedHeight = RequestedHeight;
            
            m_webCamTexture.Play();
        }

        //Create the quad for the live video stream
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = CreateSimpleQuad();
    }
    
    void Update()
    {
        if (!m_webCamTexture)
        {
            return;
        }
        
        if (m_webCamTexture.didUpdateThisFrame)
        {
            if (!m_initialized)
            {
				m_screenOrientation = Screen.orientation;
                m_currentScreenSize.Set(Screen.width, Screen.height);
                UpdateScreenOrientation();
                
                //Set game object texture to webcamera texture
                GetComponent<Renderer>().material.mainTexture = m_webCamTexture;
                
                m_frameBuffer = new Color32[Width * Height];
                
                //Tell the manager that the camera is ready
                m_initialized = true;

                if (Listener != null)
                {
                    Listener.OnCameraReady(FocalLengthInPixels, Width, Height);
                    Listener.OnNewFrameAvailable(this);
                }
            } else
            {
                if (Listener != null)
                    Listener.OnNewFrameAvailable(this);

                if (Screen.orientation != m_screenOrientation && Screen.width != 0 && Screen.width != m_currentScreenSize.x && Screen.height != 0 && Screen.height != m_currentScreenSize.y)
                {
					m_screenOrientation = Screen.orientation;
                    m_currentScreenSize.Set(Screen.width, Screen.height);
                    UpdateScreenOrientation();
                }
            }
        }
    }
    
    void OnDestroy()
    {
        m_webCamTexture.Stop();
    }
    
    #endregion
    
    /// <summary>
    /// Updates the screen orientation.
    /// </summary>
    /// <remarks>
    /// Rotate game object and adapt its scale in order for the texture be stretched at the correct
    /// ratio
    /// Define orthographic size of the camera in order for the quad to fit in height
    /// </remarks>
    public void UpdateScreenOrientation()
    {
        Vector2 onScreenCameraRatio = Vector2.one;
        float cameraRatio;
        float screenRatio;
        cameraRatio = (float)Mathf.Max(Width, Height)
            / (float)Mathf.Min(Width, Height);
        screenRatio = (float)Mathf.Max(Screen.width, Screen.height)
            / (float)Mathf.Min(Screen.width, Screen.height);

        gameObject.transform.localScale = new Vector3(1.0f, cameraRatio, 1.0f);

#if UNITY_IPHONE
		gameObject.transform.localScale = new Vector3(-1.0f, cameraRatio, 1.0f);
#endif

#if UNITY_IPHONE || UNITY_ANDROID
        switch(Screen.orientation)
        {
        case ScreenOrientation.Portrait:
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            if(screenRatio > cameraRatio) {
                onScreenCameraRatio = new Vector2(cameraRatio/screenRatio, 1f);
                CameraSource.orthographicSize = cameraRatio;
            }
            else {
                CameraSource.orthographicSize = screenRatio;
            }
            break;
        case ScreenOrientation.LandscapeLeft:
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            if(screenRatio > cameraRatio) {
                onScreenCameraRatio = new Vector2(1f, cameraRatio/screenRatio);
                CameraSource.orthographicSize = cameraRatio/screenRatio;
            }
            else {
                CameraSource.orthographicSize = 1f;
            }
            break;
        case ScreenOrientation.PortraitUpsideDown:
            transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            if(screenRatio > cameraRatio) {
                onScreenCameraRatio = new Vector2(cameraRatio/screenRatio, 1f);
                CameraSource.orthographicSize = cameraRatio;
            }
            else {
                CameraSource.orthographicSize = screenRatio;
            }
            break;
        case ScreenOrientation.LandscapeRight:
            transform.rotation = Quaternion.Euler(0f, 0f, 270f);
            if(screenRatio > cameraRatio) {
                onScreenCameraRatio = new Vector2(1f, cameraRatio/screenRatio);
                CameraSource.orthographicSize = cameraRatio/screenRatio;
            }
            else {
                CameraSource.orthographicSize = 1f;
            }
            break;
        }
#else
        transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        if (screenRatio > cameraRatio)
        {
            onScreenCameraRatio = new Vector2(1f, cameraRatio / screenRatio);
            CameraSource.orthographicSize = cameraRatio / screenRatio;
        } else
        {
            CameraSource.orthographicSize = 1f;
        }
#endif

        OnScreenCameraRatio = onScreenCameraRatio;

        if (Listener != null)
            Listener.OnScreenOrientationChanged(Width, Height, OnScreenCameraRatio);
    }
    
    /// <summary>
    /// Create the quad on which the camera texture will be rendered
    /// </summary>
    /// <returns>
    /// The mesh corresponding to the quad created
    /// </returns>
    protected Mesh CreateSimpleQuad()
    {
        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];
        Mesh simpleQuad = new Mesh();
        
        vertices [0] = Vector3.up + Vector3.left;
        vertices [1] = Vector3.up + Vector3.right;
        vertices [2] = Vector3.down + Vector3.right;
        vertices [3] = Vector3.down + Vector3.left;
        
        for (int i = 0; i < 4; i++)
            normals [i] = Vector3.forward;
        
        uv [0] = Vector2.zero;
        uv [1] = Vector2.up;
        uv [2] = Vector2.up + Vector2.right;
        uv [3] = Vector2.right;
        
        triangles [0] = 0;
        triangles [1] = 1;
        triangles [2] = 2;
        triangles [3] = 0;
        triangles [4] = 2;
        triangles [5] = 3;
        
        simpleQuad.vertices = vertices;
        simpleQuad.normals = normals;
        simpleQuad.uv = uv;
        simpleQuad.triangles = triangles;
        
        return simpleQuad;
    }
}
