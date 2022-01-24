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
    StateDetection stateDetection;
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
        stateDetection = new StateDetection();
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
        //Texture2D texture = objectDetection.DetectObjects(mat);
        int[,] grid = stateDetection.detectState(mat);


        //background.texture = texture;
        // ---

        stopwatch.Stop();
        if (debugFps && stopwatch.ElapsedMilliseconds != 0)
        {
            Debug.Log(stopwatch.ElapsedMilliseconds);
        }
        if (this.debug)
        {
            printGrid(grid);
        }
    }

    private void printGrid(int[,] grid)
    {
        Debug.Log("#############");
        string grid_str = "";
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                grid_str += grid[i, j] + "\t";
            }
            grid_str += "\n";
        }
        Debug.Log(grid_str);
    }
}
