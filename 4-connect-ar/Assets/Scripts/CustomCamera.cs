using OpenCvSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomCamera
{
    private bool isCameraAvailable;
    private WebCamTexture cameraTexture;
    private RawImage canvas;
    private AspectRatioFitter fitter;

    public int Width;
    public int Height;

    public CustomCamera(RawImage canvas, int width, int height)
    {
        this.canvas = canvas;
        this.Width = width;
        this.Height = height;
        InitializeCamera();
    }

    public void SetColors(Color32[] colors)
    { 
    }

    public void Refresh()
    {
        canvas.texture = cameraTexture;
    }

    public void RefreshCameraRatio()
    {
        if (!IsCameraAvailable())
        {
            return;
        }

        float ratio = (float)cameraTexture.width / (float)cameraTexture.height;
        //fitter.aspectRatio = ratio;
        fitter.aspectRatio = ratio;

        // Ist das Handy gedreht
        float scaleY = cameraTexture.videoVerticallyMirrored ? -1f : 1f;
        canvas.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        int orient = -cameraTexture.videoRotationAngle;
        canvas.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
    }

    public bool IsCameraAvailable()
    {
        return isCameraAvailable;
    }

    public Mat GetCurrentFrameAsMat()
    {
        if (canvas.texture == null)
        {
            return null;
        }

        return OpenCvHelper.ConvertImageToMat(canvas);
    }

    private void InitializeCamera()
    {
        fitter = canvas.GetComponent<AspectRatioFitter>();

        WebCamDevice[] devices = WebCamTexture.devices;

        // At least 1 cam
        if (devices.Length == 0)
        {
            Debug.Log("No Camera Detected");
            isCameraAvailable = false;
            return;
        }

        // At least 1 back cam
        Debug.Log("Amount of cams found: " + devices.Length);
        foreach (WebCamDevice device in devices)
        {
            if (!device.isFrontFacing)
            {
                cameraTexture = new WebCamTexture(device.name, Width, Height);
            }
        }

        // Backcam isnt null
        if (cameraTexture == null)
        {
            Debug.Log("Unable to find back camera");

            // No BackCam found ... 
            // Use first possible cam
            cameraTexture = new WebCamTexture(devices[0].name, Width, Height);
        }

        // Activate backCam
        Debug.Log("Activate Cam: " + cameraTexture.name);
        cameraTexture.Play();
        canvas.texture = cameraTexture;

        RefreshCameraRatio();

        isCameraAvailable = true;
    }
}

