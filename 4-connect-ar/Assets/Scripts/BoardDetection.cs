using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardDetection : MonoBehaviour
{
    public RawImage background; // Theorie: Nur das Cam-Bild
    public RawImage overlay; // Theorie: Hier nur die Rechtecke zeichnen, ohne Cam-Bild
    public int Width = 640;
    public int Height = 480;

    ObjectDetection objectDetection;
    CustomCamera camera;

    // Debug
    public bool debug;
    public bool debugFps;
    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    void Start()
    {
        // https://www.nuget.org/packages/OpenCvSharp4/
        // https://www.tech-quantum.com/have-fun-with-webcam-and-opencv-in-csharp-part-1/
        // https://www.tech-quantum.com/have-fun-with-webcam-and-opencv-in-csharp-part-2/

        Screen.SetResolution(Width, Height, FullScreenMode.Windowed);
        camera = new CustomCamera(background, Width, Height);
        objectDetection = new ObjectDetection();
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        // Time
        stopwatch.Restart();
        camera.Refresh();

        // Objekterkennung durchführen
        Mat mat = camera.GetCurrentFrameAsMat();
        Texture2D texture = objectDetection.DetectObjects(mat);
        background.texture = texture;
        // ---

        stopwatch.Stop();
        if (debugFps && stopwatch.ElapsedMilliseconds != 0)
        {
            Debug.Log(stopwatch.ElapsedMilliseconds);
        }
    }
}
