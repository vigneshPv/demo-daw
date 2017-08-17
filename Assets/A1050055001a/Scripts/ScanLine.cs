using UnityEngine;
using System.Collections;
using A1050055001a;
using System;
using System.Threading;
using System.Linq;

/// <summary>
/// Render a scanning line on GUI which shows dots on his trail
/// </summary>
/// <remarks>
/// Based on Fast algorithm and using GUI textures with alpha, may be performance costly
/// It has been implemented only for portrait orientation
/// </remarks>
public class ScanLine : MonoBehaviour
{
    
    /// <summary>
    /// Keeps track if a new process of the screen can be done
    /// </summary>
    private bool m_ready = false;
    
    /// <summary>
    /// The last position on y of the scanning line
    /// </summary>
    private float m_scanPositionY = 1f;
    
    /// <summary>
    /// The maximum number of dots rendered on the screen each frame
    /// </summary>
    private const int m_maxDotOnScreen = 500;
    
    /// <summary>
    /// Buffer of dots elements to avoid instantiation at runtime
    /// </summary>
    private GameObject[] m_dotBuffer;
    
    /// <summary>
    /// The number of dots found during the last process of the screen
    /// </summary>
    private int m_nbLastDots = 0;
    
    /// <summary>
    /// The lock to protect concurrency access to the buffer of dot elements
    /// </summary>
    private object m_dotBufferLock = new object();
    
    /// <summary>
    /// The proportion of the screen to scan above and under the scanning line
    /// </summary>
    private const float m_areaToScan = 1f / 5f;

	/// <summary>
	/// To use the dots points (much slower).
	/// </summary>
	public bool UseDotsPoints = true;
    
    /// <summary>
    /// The dot GUI Texture object to be rendered
    /// </summary>
    /// <remarks>
    /// Should be set in the Unity Editor interface
    /// </remarks>
    public GameObject m_dotObject;
    
    /// <summary>
    /// The desired color to apply to the dot
    /// </summary>
    /// <remarks>
    /// Should be set in the Unity Editor interface
    /// </remarks>
    public Color m_dotColor;

    /// <summary>
    /// Looking for dots at every frame can slow the performance. 
    /// You can set how many frames to skipped between to extraction of dots.
    /// </summary>
    public int m_framesSkip = 5;
    private int m_skippedFrames = 0;
    
    /// <summary>
    /// The timer used to update the position of the scanning line
    /// </summary>
    private float m_timer = 0f;
    
    /// <summary>
    /// Stores dots position after the scan process of the area.
    /// </summary>
    private Vector2[] m_dotsPosition;
    
    #region MonoBehaviour specific
    
    /// <summary>
    /// Monobehaviour Start method.
    /// Used here to initialize the scanning line and bufferize the dots elements
    /// </summary>
    /// <remarks>
    /// See Unity documentation for more information on this method
    /// </remarks>
    void Start()
    {
        StartCoroutine(WaitForCamera());
    }
    
    /// <summary>
    /// Monobehaviour OnDisable method.
    /// Used here to reset the state of the scanning line to restart at the top of the screen when
    /// re-enabled
    /// </summary>
    /// <remarks>
    /// See Unity documentation for more information on this method
    /// </remarks>
    void OnDisable()
    {
        //Dots are deactivated and state is reset to start at the top of the screen
        if (m_dotBuffer != null)
        {
            for (int i=0; i<m_maxDotOnScreen; ++i)
            {
                if (m_dotBuffer [i] != null)
                {
                    m_dotBuffer [i].SetActive(false);
                }
            }
        }
        m_nbLastDots = 0;
        transform.position = new Vector3(0f, 1f, 0f);
        m_scanPositionY = 1f;
        m_timer = 0f;
    }
    
    /// <summary>
    /// Monobehaviour OnDestroy method.
    /// Used here to destroy the dot elements in the buffer
    /// </summary>
    /// <remarks>
    /// See Unity documentation for more information on this method
    /// </remarks>
    void OnDestroy()
    {
        if (m_dotBuffer != null)
        {
            for (int i=0; i<m_maxDotOnScreen; ++i)
            {
                if (m_dotBuffer [i] != null)
                {
                    Destroy(m_dotBuffer [i]);
                    m_dotBuffer [i] = null;
                }
            }
        }

        m_dotBuffer = null;
    }
    
    /// <summary>
    /// Monobehaviour Update method.
    /// Used here to update the state of the dots on screen and relaunch a process if the last one
    /// is done
    /// </summary>
    /// <remarks>
    /// See Unity documentation for more information on this method
    /// </remarks>
    void Update()
    {
        ++m_skippedFrames;

        //Set the GUI texture of the scanning line to fit in width
        Rect newPixelInset = GetComponent<GUITexture>().pixelInset;
        newPixelInset.width = Screen.width;
        GetComponent<GUITexture>().pixelInset = newPixelInset;

        //Calculate new scanning line position
        m_timer += Time.deltaTime;
        float newScanPosY = Mathf.PingPong(5f + m_timer, 5f) / 5f;
        bool goingDown = (m_scanPositionY < newScanPosY) ? false : true;
        
        m_scanPositionY = newScanPosY;
        
        transform.position = new Vector3(0f, m_scanPositionY, 0f);

        if (m_dotBuffer != null)
        {
            //Update dot state on screen
            lock (m_dotBufferLock)
            {
                for (int i=0; i<m_maxDotOnScreen; ++i)
                {
                    if (i < m_nbLastDots)
                    {
                        m_dotBuffer [i].SetActive(true);
                        m_dotBuffer [i].transform.position = m_dotsPosition [i];

                        if (goingDown)
                        {
                            if (m_dotsPosition [i].y < m_scanPositionY)
                            {
                                m_dotColor.a = 0f;
                            } else
                            {
                                m_dotColor.a = Mathf.Clamp01(1f - (m_dotsPosition [i].y - m_scanPositionY) / m_areaToScan) * 0.5f;
                            }
                        } else
                        {
                            if (m_dotsPosition [i].y > m_scanPositionY)
                            {
                                m_dotColor.a = 0f;
                            } else
                            {
                                m_dotColor.a = Mathf.Clamp01(1f - (m_scanPositionY - m_dotsPosition [i].y) / m_areaToScan) * 0.5f;
                            }
                        }
                        m_dotBuffer [i].GetComponent<GUITexture>().color = m_dotColor;
                    } else
                    {
                        m_dotBuffer [i].SetActive(false);
                    }
                }
            }
        }
        
        //If there is already an ongoing process, stop, otherwise launch a new process
        if (!m_ready)
        {
            return;
        }

        if (m_dotBuffer != null && m_skippedFrames >= m_framesSkip)
        {
            m_ready = false;
            m_skippedFrames = 0;

            //Determine the new area to scan
            Rect frameArea = new Rect(((1f - m_scanPositionY) - m_areaToScan), 0f, m_areaToScan * 2f, 1f);
            if (frameArea.x + frameArea.width > 1f)
            {
                frameArea.width = 1f - frameArea.x;
            } else if (frameArea.x < 0f)
            {
                frameArea.width += frameArea.x;
                frameArea.x = 0f;
            }

            Color32[] frame = CameraRenderer.Instance.GetFrame();
            frame = Utils.FlipVertically(frame, CameraRenderer.Instance.Width, null);                    
           
            //Process the new area to scan
            RunAsync(() =>
            {
                Vector2[] dotsPosition = DotFinder.FindPoints(frameArea, m_maxDotOnScreen, frame);
                lock (m_dotBufferLock)
                {
                    m_dotsPosition = dotsPosition;
                    m_nbLastDots = (m_dotsPosition != null) ? Mathf.Min(m_dotsPosition.Length, m_maxDotOnScreen) : 0;
                    m_ready = true;
                }
            });
        }
    }

    public static void RunAsync(Action a)
    {
        Thread t = new Thread(RunAction);
        t.IsBackground = true;
        t.Start(a);
    }
    
    private static void RunAction(object action)
    {
        ((Action)action)();
    }
    
    #endregion
    
    private IEnumerator WaitForCamera()
    {
        int cameraWidth = 0;
        int cameraHeight = 0;
        do
        {
            yield return null;
            cameraWidth = CameraRenderer.Instance.Width;
            cameraHeight = CameraRenderer.Instance.Height;
        } while(cameraWidth <=0 || cameraHeight <=0);

        if (Manager.Instance.NativeAvailable && UseDotsPoints)
        {
            //Bufferize the dots elements
            GameObject parent = new GameObject("Dots");
            m_dotBuffer = new GameObject[m_maxDotOnScreen];
            for (int i=0; i<m_maxDotOnScreen; ++i)
            {
                m_dotBuffer [i] = (GameObject)Instantiate(m_dotObject, Vector3.zero, Quaternion.identity);
                m_dotBuffer [i].SetActive(false);
                m_dotBuffer [i].transform.parent = parent.transform;
            }

            DotFinder.Initialize(cameraWidth, cameraHeight);
        }

        m_ready = true;
    }
}
