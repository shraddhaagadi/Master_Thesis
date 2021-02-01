// Author: Shraddha Agadi
// Email: shraddha.agadi@hs-weingarten.de

using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AndroidWebcamDisplay : MonoBehaviour
{
    private Nullable<WebCamDevice> webCameraDevice = null;

    private WebCamTexture webCameraTexture;

	// save the webcam texture
	private Material camTextureHolder;

	// shader of implementing AR effect
	public Material shaderMaterial;

    public GameObject Surface;

    // a null RenderTexture object to hold place in Graphics.Blit function
    private RenderTexture nullRenderTexture = null;

	// width and height of actual webcam texture
	private int webcamWidth, webcamHeight;

	// fps counter
	const float fpsMeasurePeriod = 0.5f;
	private int m_FpsAccumulator = 0;
	private float m_FpsNextPeriod = 0;
	private int m_CurrentFps;
	private int fontSize = 40;

    // values controlling the image processing
	[Range(1.4f, 1.7f)]
	public float FOV = 1.6f;
	[Range(0.0f, 0.3f)]
	public float Disparity = 0.1f;
    [Range(0.0f, 0.5f)]
    public float Magnifier;
    [Range(0.6f, 3.6f)]
	public float Zoom;
    [Range(0.0f, 0.3f)]
    public float BulgeControl;

    public float x;
    public float y;
    public float disp;
    public float fov;

    // switch between gui and bluetooth controls
    private bool guiControl;

    private int numDevices = 0;

    // variables for camera selection dropdown menu
    private Vector2 scrollViewVector = Vector2.zero;
    private ArrayList devices = new ArrayList();
    int n, i, deviceID;



    // Use this for initialization
    void Awake() 
	{
        numDevices = WebCamTexture.devices.Length;
        n = 0; i = 0; deviceID = 0;

        x = 0.0f; y = 0.0f;

        guiControl = false;
        Zoom = 0.6f;
        Magnifier = 0.0f;

        // Never turn off the screen
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
		m_FpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;

		// camTextureHolder needs to be initialized before using
		camTextureHolder = new Material(shaderMaterial);

        // Checks how many and which cameras are available on the device
        for (int cameraIndex = 0; cameraIndex < numDevices; cameraIndex++) 
		{
            devices.Add(WebCamTexture.devices[cameraIndex].name);
		}

        /* Available resolutions on Samsung S10e
         * 3840x2160, 1920x1080, 1280x720 */
        //      webCameraTexture = new WebCamTexture(deviceID, Screen.width, Screen.height);
        //      //webCameraTexture = new WebCamTexture((string)devices[deviceID]);

        //      camTextureHolder.mainTexture = webCameraTexture;
        //webCameraTexture.Play();
        setWebCam(WebCamTexture.devices[deviceID].name);

		webcamWidth = webCameraTexture.width;
		webcamHeight = webCameraTexture.height;

		Debug.Log("WebcamTexture width = " + webcamWidth + ", height = " + webcamHeight);

		// Alpha is the pixel density ration of width over height
		// Needed for displaying the final image without skew
		float Alpha = (float)webcamHeight / (float)Screen.height * (float)Screen.width * 0.5f / (float)webcamWidth;
		shaderMaterial.SetFloat("_Alpha", Alpha);
	}

    private void setWebCam(string deviceName)
    {
        if (null != webCameraTexture && webCameraTexture.isPlaying)
            webCameraTexture.Stop();

        // get device index
        int cameraIndex = -1;
        for (int i = 0; i < WebCamTexture.devices.Length && -1 == cameraIndex; i++)
        {
            if (WebCamTexture.devices[i].name == deviceName)
                cameraIndex = i;
        }

        // set device up
        if (-1 != cameraIndex)
        {
            webCameraDevice = WebCamTexture.devices[cameraIndex];
            //webCameraTexture = new WebCamTexture(webCameraDevice.Value.name);
            webCameraTexture = new WebCamTexture(cameraIndex, Screen.width, Screen.height);
            camTextureHolder.mainTexture = webCameraTexture;
            webCameraTexture.Play();
        }
        else
        {
            throw new ArgumentException(String.Format("{0}: provided DeviceName is not correct device identifier", this.GetType().Name));
        }
    }


    // Bluetooth inputs are updated once per frame if active
    private void UpdateBluetoothInputs()
    {
        #region ZOOM
        float zoo = Input.GetAxis("Axis 1"); // zoo values are between -1 and 1
        float temp = zoo + 1; // for zoo = 1, temp = 2;
        if (zoo > 0f)
        {
            if (Zoom < temp) // zoom values are between 1.0 and 1.4
            {
                Zoom += zoo * 0.01f;
                if (Zoom > 3.6f)
                    Zoom = 3.6f;
            }

        }

        else if (zoo < 0f)
        {
            if (temp < Zoom)  // for zoom = 1.4 and temp = 0.5, 0 and zoo = -0.5, -1
            {
                Zoom += zoo * 0.01f;
                if (Zoom < 0.6f)
                    Zoom = 0.6f;
            }
        }
        #endregion

        #region BULGE
        float mag = Input.GetAxis("Axis 2") / 2;

        if (mag > 0f)
        {
            if (Magnifier < mag)
            {
                Magnifier += mag * 0.01f;

                if (Magnifier > 0.5f)
                    Magnifier = 0.5f;
            }

        }

        else if (mag < 0f)
        {
            if (mag < Magnifier)
            {
                Magnifier += mag * 0.01f;

                if (Magnifier < 0)
                    Magnifier = 0;
            }
        }
        #endregion

        #region DISPARITY
        if (Input.GetKeyUp(KeyCode.JoystickButton0) == true)
        {
            // disparity range (0-0.3) so to provide 5 disparity levels 0.3/5 = 0.06 
            Disparity = Disparity - 0.006f;
            if (Disparity < 0)
            {
                Disparity = 0;

            }

        }

        if (Input.GetKeyUp(KeyCode.JoystickButton3) == true)
        {
            Disparity = Disparity + 0.006f;
            if (Disparity > 0.3f)
            {
                Disparity = 0.3f;
            }

        }
        #endregion

        #region BulgeControl
        if (Input.GetKeyUp(KeyCode.JoystickButton1) == true)
        {
            // disparity range (0-0.3) so to provide 5 disparity levels 0.3/5 = 0.06 
            BulgeControl = BulgeControl - 0.006f;
            if (BulgeControl < 0)
            {
                BulgeControl = 0;

            }

        }

        if (Input.GetKeyUp(KeyCode.JoystickButton2) == true)
        {
            BulgeControl = BulgeControl + 0.006f;
            if (BulgeControl > 0.3f)
            {
                BulgeControl = 0.3f;
            }

        }
        #endregion

        //#region FOV
        //if (Input.GetKeyUp(KeyCode.JoystickButton1) == true)
        //{
        //    FOV = FOV - 0.01f;
        //    if (FOV < 1)
        //    {
        //        FOV = 1;
        //    }
        //}

        //if (Input.GetKeyUp(KeyCode.JoystickButton2) == true)
        //{
        //    FOV = FOV + 0.01f;
        //    if (FOV > 1.7f)
        //    {
        //        FOV = 1.7f;
        //    }
        //}
        //#endregion
    }

    // GUI inputs are updated once per frame if active
    private void UpdateGUIInputs()
    {
        int labelHeight = 40;
        int boundary = 20;

        #region SLIDERS
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(boundary, Screen.height - boundary - labelHeight, 400, labelHeight), webcamWidth + " x " + webcamHeight + "  " + m_CurrentFps + "fps");

        GUI.Label(new Rect(Screen.width - boundary - 200, boundary, 200, labelHeight), "FOV");
        FOV = GUI.HorizontalSlider(new Rect(Screen.width - boundary - 200, boundary + labelHeight, 200, labelHeight), FOV, 1.4F, 1.7F);

        GUI.Label(new Rect(Screen.width - boundary - 200, Screen.height - labelHeight * 2 - boundary, 200, labelHeight), "Disparity");
        Disparity = GUI.HorizontalSlider(new Rect(Screen.width - boundary - 200, Screen.height - labelHeight - boundary, 200, labelHeight), Disparity, 0.0F, 0.3F);

        GUI.Label(new Rect(Screen.width - boundary - 200, Screen.height / 2 - labelHeight, 200, labelHeight), "Magnifier");
        Magnifier = GUI.HorizontalSlider(new Rect(Screen.width - boundary - 200, Screen.height / 2, 200, labelHeight), Magnifier, 0.0F, 0.5F);

        GUI.Label(new Rect(Screen.width - boundary - 1200, Screen.height / 2 - labelHeight, 200, labelHeight), "Zoom");
        Zoom = GUI.HorizontalSlider(new Rect(Screen.width - boundary - 1200, Screen.height / 2, 200, labelHeight), Zoom, 0.6F, 3.6F);
                                //0, Screen.height / 2, 300, 60
        GUI.Label(new Rect(0, Screen.height / 2, 3000, 100), "BulgeControl");
        BulgeControl = GUI.HorizontalSlider(new Rect(0, Screen.height / 2 + 120, 200, 100), BulgeControl, 0.0F, 0.3F);
        #endregion

        //#region CAMERA_SELECTION_DROPDOWN
        //if (GUI.Button(new Rect(0, 80, 300, 60), "Switch Camera"))
        //{
        //    if (n == 0) n = 1;
        //    else n = 0;
        //}

        //if (n == 1)
        //{
        //    scrollViewVector = GUI.BeginScrollView(new Rect(0, 150, 300, 240), scrollViewVector, new Rect(0, 0, 300, 500));
        //    GUI.Box(new Rect(0, 0, 300, 500), "");
        //    for (i = 0; i < numDevices; i++)
        //    {
        //        if (GUI.Button(new Rect(0, i * 60, 300, 60), ""))
        //        {
        //            n = 0; deviceID = i;
        //            //SwitchCamera(deviceID);
        //            setWebCam(WebCamTexture.devices[deviceID].name);
        //        }
        //        GUI.Label(new Rect(5, i * 60, 300, 60), (string)devices[i]);
        //    }
        //    GUI.EndScrollView();

        //}
        //else
        //{
        //    GUI.Label(new Rect(0, 140, 300, 60), (string)devices[deviceID]);
        //}
        //#endregion
    }


    private void SwitchCamera(int ID)
    {
        webCameraTexture.Stop();
        webCameraTexture.deviceName = (string)devices[ID];
        webCameraTexture.Play();
    }


    void OnGUI() 
	{ 
		GUI.skin.label.fontSize = GUI.skin.box.fontSize = GUI.skin.button.fontSize = fontSize;

        #region CONTROLS_SWITCH
        // A button to switch between bluetooth and gui controls
        if (GUI.Button(new Rect(0, 0, 200, 60), "ON/OFF")) 
		{
            // if guiControl is true make it false
            // if guiControl is false make it true
            if (guiControl == false)
            {
                guiControl = true;
            }
            else
            {
                guiControl = false;
            }
        }
        #endregion

        #region GUI_CONTROL
        if (guiControl == true)
        {
            UpdateGUIInputs();
        }
        #endregion

        #region BLUETOOTH_CONTROL
        else
        {
            UpdateBluetoothInputs();
        }
        #endregion

        #region SET_SHADER_VALUES
        shaderMaterial.SetFloat("_Zoom", Zoom);
        shaderMaterial.SetFloat("_Magnifier", Magnifier);
        shaderMaterial.SetFloat("_FOV", FOV);
        shaderMaterial.SetFloat("_Disparity", Disparity);
        shaderMaterial.SetFloat("_BulgeControl", BulgeControl);
        #endregion

        // GUI.Label(new Rect(0, Screen.height / 2, 300, 60), "Disparity: " + Disparity);
        //GUI.Label(new Rect(0, Screen.height / 2 + 60, 300, 60), "FOV: " + FOV);  
    }


    //   void OnRenderImage(RenderTexture src, RenderTexture dest) 
    //{
    //	// shaderMaterial renders the image with Barrel distortion and disparity effect
    //	Graphics.Blit(camTextureHolder.mainTexture, nullRenderTexture, shaderMaterial);

    //	// measure average frames per second
    //	m_FpsAccumulator++;
    //	if (Time.realtimeSinceStartup > m_FpsNextPeriod) 
    //	{
    //		m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
    //		m_FpsAccumulator = 0;
    //		m_FpsNextPeriod += fpsMeasurePeriod;
    //	}
    //}


    private void Update()
    {
        if (webCameraTexture != null && webCameraTexture.didUpdateThisFrame)
        {
            RenderFrame();
            Debug.Log("updated");
        }
    }

    private void RenderFrame()
    {
        if (webCameraTexture != null)
        {
            // apply
            Surface.GetComponent<RawImage>().texture = webCameraTexture;

            // Adjust image ration according to the texture sizes 
            Surface.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);

            // measure average frames per second
            m_FpsAccumulator++;
            if (Time.realtimeSinceStartup > m_FpsNextPeriod)
            {
                m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
                m_FpsAccumulator = 0;
                m_FpsNextPeriod += fpsMeasurePeriod;
            }

            Debug.Log("frame rendered");
        }
    }

}
