
using System;
using UnityEngine;
using UnityEngine.UI;

public class WEBCAM : MonoBehaviour
{
    /// <summary>
    /// Target surface to render WebCam stream
    /// </summary>
    public GameObject Surface;

    private Nullable<WebCamDevice> webCamDevice = null;
    private WebCamTexture webCamTexture = null;
    private Texture2D renderedTexture = null;

    /// <summary>
    /// Camera device name, full list can be taken from WebCamTextures.devices enumerator
    /// </summary>
    public string DeviceName
    {
        get
        {
            return (webCamDevice != null) ? webCamDevice.Value.name : null;
        }
        set
        {
            // quick test
            if (value == DeviceName)
                return;

            if (null != webCamTexture && webCamTexture.isPlaying)
                webCamTexture.Stop();

            // get device index
            int cameraIndex = -1;
            for (int i = 0; i < WebCamTexture.devices.Length && -1 == cameraIndex; i++)
            {
                if (WebCamTexture.devices[i].name == value)
                    cameraIndex = i;
            }

            // set device up
            if (-1 != cameraIndex)
            {
                webCamDevice = WebCamTexture.devices[cameraIndex];
                webCamTexture = new WebCamTexture(webCamDevice.Value.name);

                webCamTexture.Play();
            }
            else
            {
                throw new ArgumentException(String.Format("{0}: provided DeviceName is not correct device identifier", this.GetType().Name));
            }
        }
    }

       
    /// <summary>
    /// Default initializer for MonoBehavior sub-classes
    /// </summary>
    protected virtual void Awake()
    {
        bool deviceHasBeenSet = false;
        if (WebCamTexture.devices.Length > 0)
        {
            for (int i = 0; i < WebCamTexture.devices.Length; ++i)
            {
                if (!WebCamTexture.devices[i].isFrontFacing)
                {
                    DeviceName = WebCamTexture.devices[i].name;
                    deviceHasBeenSet = true;
                }
            }

        }

        if (!deviceHasBeenSet && WebCamTexture.devices.Length > 0)
            DeviceName = WebCamTexture.devices[WebCamTexture.devices.Length - 1].name;

        Debug.Log("Camera Used: " + DeviceName);
    }

    void OnDestroy()
    {
        if (webCamTexture != null)
        {
            if (webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
            webCamTexture = null;
        }

        if (webCamDevice != null)
        {
            webCamDevice = null;
        }
    }

    /// <summary>
    /// Updates web camera texture
    /// </summary>
    private void Update()
    {
        if (webCamTexture != null && webCamTexture.didUpdateThisFrame)
        {
            RenderFrame();
            Debug.Log("updated");
        }
    }


    /// <summary>
    /// Renders frame onto the surface
    /// </summary>
    //private void RenderFrame()
    //{
    //    if (renderedTexture != null)
    //    {
    //        // apply
    //        Surface.GetComponent<RawImage>().texture = renderedTexture;

    //        // Adjust image ration according to the texture sizes 
    //        Surface.GetComponent<RectTransform>().sizeDelta = new Vector2(renderedTexture.width, renderedTexture.height);

    //        Debug.Log("frame rendered");
    //    }
    //}
    private void RenderFrame()
    {
        if (webCamTexture != null)
        {
            // apply
            Surface.GetComponent<RawImage>().texture = webCamTexture;

            // Adjust image ration according to the texture sizes 
            Surface.GetComponent<RectTransform>().sizeDelta = new Vector2(webCamTexture.width, webCamTexture.height);

            Debug.Log("frame rendered");
        }
    }
}