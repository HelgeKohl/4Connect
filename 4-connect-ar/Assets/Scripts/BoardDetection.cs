using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class BoardDetection : MonoBehaviour
{
    public RawImage background; // Theorie: Nur das Cam-Bild
    public RawImage overlay; // Theorie: Hier nur die Rechtecke zeichnen, ohne Cam-Bild
    public int Width = 640;
    public int Height = 480;
    public BaseAgent Agent;

    private ObjectDetection objectDetection;
    private StateDetection stateDetection;
    private Board board;
    private CustomCamera camera;
    private Thread cv2WorkerThread;
    private ConcurrentStack<Color32[]> stack = new ConcurrentStack<Color32[]>();
    private readonly object lockObj = new object();
    private Mat threadResponseMat;
    private Mat threadInputMat;

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
        board = new Board();
        Agent.Board = board;

        Texture.allowThreadedTextureCreation = true;
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        // Time
        stopwatch.Restart();

        camera.Refresh();
        threadInputMat = camera.GetCurrentFrameAsMat();
        TryAddCurrentMat();

        // here in the main thread work the stack
        if (stack.TryPop(out var pixels32))
        {
            // Only use SetPixels and Apply when really needed
            //Texture2D texture2D = background.texture as Texture2D;
            if (OpenCvHelper.Overlay != null)
            {
                background.texture = OpenCvHelper.Overlay;
            }
        }

        stack.Clear();
        // ---

        stopwatch.Stop();
        if (debugFps && stopwatch.ElapsedMilliseconds != 0)
        {
            Debug.Log(stopwatch.ElapsedMilliseconds);
        }
    }

    private void printGrid(int[,] grid)
    {
        Debug.Log("#############");
        string grid_str = "";
        for (int i = 0; i < 7; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                grid_str += grid[i, j] + "\t";
            }
            grid_str += "\n";
        }
        Debug.Log(grid_str);
    }

    void OnEnable()
    {
        stack.Clear();

        if (cv2WorkerThread != null)
        {
            cv2WorkerThread.Abort();
        }

        cv2WorkerThread = new Thread(CalculateOpenCvWork);
        cv2WorkerThread.Start();
    }

    // Make sure to terminate the thread everytime this object gets disabled
    private void OnDisable()
    {
        if (cv2WorkerThread == null) return;

        cv2WorkerThread.Abort();
        cv2WorkerThread = null;
    }

    /// <summary>
    /// Data struct to hold raw image data
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct Color32Bytes
    {
        [FieldOffset(0)]
        public byte[] byteArray;
        [FieldOffset(0)]
        public Color32[] colors;
    }

    private void TryAddCurrentMat()
    {
        lock (lockObj)
        {
            if (threadResponseMat == null)
            {
                return;
            }

            Color32[] colors = GetColors(threadResponseMat);
            threadResponseMat.Dispose();
            stack.Push(colors);

            threadResponseMat = null;
        }
    }

    private Color32[] GetColors(Mat mat)
    {
        OpenCvHelper.Overlay = OpenCvSharp.Unity.MatToTexture(mat);
        Color32[] colors = OpenCvHelper.Overlay.GetPixels32();

        return colors;
    }

    // TODO überflüssig, wenn Grid anders zurückgegeben wird
    public int[,] TransposeRowsAndColumns(int[,] arr)
    {
        int rowCount = arr.GetLength(0);
        int columnCount = arr.GetLength(1);
        int[,] transposed = new int[columnCount, rowCount];
        if (rowCount == columnCount)
        {
            transposed = (int[,])arr.Clone();
            for (int i = 1; i < rowCount; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    int temp = transposed[i, j];
                    transposed[i, j] = transposed[j, i];
                    transposed[j, i] = temp;
                }
            }
        }
        else
        {
            for (int column = 0; column < columnCount; column++)
            {
                for (int row = 0; row < rowCount; row++)
                {
                    transposed[column, row] = arr[row, column];
                }
            }
        }
        return transposed;
    }

    // Runs in a thread!
    void CalculateOpenCvWork()
    {
        while (true)
        {
            try
            {
                if (camera == null || threadInputMat == null || threadResponseMat != null)
                {
                    continue;
                }

                Mat matObjects = objectDetection.DetectObjects(threadInputMat);
                // TODO: bei detectState statt mat nur noch das Teil-Rect aus DetectObjects übergeben
                int[,] grid = stateDetection.detectState(threadInputMat);

                // Status übergeben
                // TODO: Hat wahrscheinlich die falsche Dimension
                // TransposeRowsAndColumns muss unnötig gemacht werden
                board.State = TransposeRowsAndColumns(grid);

                // Statt true: Wenn sich das Grid zum vorherigen Status geändert hat
                bool gridStateHasChanged = true;
                if (gridStateHasChanged)
                {
                    Agent.RequestDecision();
                }

                // Was soll angezeigt werden
                threadResponseMat = matObjects;

                if (this.debug)
                {
                    printGrid(board.State);
                }
            }
            catch (ThreadAbortException ex)
            {
                // This exception is thrown when calling Abort on the thread
                // -> ignore the exception since it is produced on purpose
            }
        }
    }
}
