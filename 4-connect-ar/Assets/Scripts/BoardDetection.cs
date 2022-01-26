using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    private new CustomCamera camera;
    private Thread cv2WorkerThread;
    private readonly object lockObj = new object();
    private Mat threadResponseMat;
    private Mat threadInputMat;

    // Position Hover-Column
    private float posX;
    private float posY;

    // WinStates
    public GameObject YellowWon;
    public GameObject RedWon;
    public GameObject Draw;

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
        board.redChip = stateDetection.id_red;
        board.yellowChip = stateDetection.id_yellow;
        board.UpdateWinstate();
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

        if (OpenCvHelper.Overlay != null)
        {
            background.texture = OpenCvHelper.Overlay;
        }

        ShowWinState(board.WinState);
        // ---

        stopwatch.Stop();
        if (debugFps && stopwatch.ElapsedMilliseconds != 0)
        {
            Debug.Log(stopwatch.ElapsedMilliseconds);
        }
    }

    void OnEnable()
    {
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
            threadResponseMat = null;
        }
    }

    private Color32[] GetColors(Mat mat)
    {
        OpenCvHelper.Overlay = OpenCvSharp.Unity.MatToTexture(mat);
        Color32[] colors = OpenCvHelper.Overlay.GetPixels32();

        return colors;
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

                BoardOperations regionSelectionOperation = BoardOperations.CropOuterRegion;
                Mat boardRegion = objectDetection.DetectObjects(threadInputMat, regionSelectionOperation);

                if (boardRegion == null)
                {
                    continue;
                }

                // TODO: bei detectState statt mat nur noch das Teil-Rect aus DetectObjects übergeben
                StateResult result = stateDetection.detectState(boardRegion == null || regionSelectionOperation == BoardOperations.Highlight ? threadInputMat : boardRegion);

                // Prüfe ob State sich geändert hat
                bool gridStateHasChanged = stateChanged(result.State, board.State);

                if (!result.isValid)
                {
                    //board.Reset();
                    //board.UpdateWinstate();
                }
                else
                {
                    // aktualisieren des States
                    board.State = result.State;

                    if (gridStateHasChanged && result.isValid)
                    {
                        board.CurrentPlayer = result.CountRedChips > result.CountYellowChips ? Player.Yellow : Player.Red;
                        Agent.RequestDecision();
                        board.UpdateWinstate();
                    }
                }

                // Was soll angezeigt werden
                //threadResponseMat = boardRegion;

                if (this.debug)
                {
                    board.printGrid();
                }
            }
            catch (ThreadAbortException)
            {
                // This exception is thrown when calling Abort on the thread
                // -> ignore the exception since it is produced on purpose
            }
        }
    }

    //  Quelle: https://stackoverflow.com/questions/12446770/how-to-compare-multidimensional-arrays-in-c-sharp user287107 Antwort 1
    /// <summary>
    /// Vergleich zweier int[,] Grids
    /// <param name="inputGrid1">Erstes Grid</param>
    /// <param name="inputGrid2">Zweites Grid</param>
    /// <returns>Gleichheit der beiden Grids</returns>
    /// </summary>
    private bool stateChanged(int[,] inputGrid1, int[,] inputGrid2)
    {
        bool equal = (
            inputGrid1.Rank == inputGrid2.Rank &&
            Enumerable.Range(0, inputGrid1.Rank).All(dimension => inputGrid1.GetLength(dimension) == inputGrid2.GetLength(dimension)) &&
            inputGrid1.Cast<int>().SequenceEqual(inputGrid2.Cast<int>())
        );

        return !equal;
    }

    public void HoverColumn(int columnIndex)
    {

    }

    public void ShowWinState(WinState winState)
    {
        RedWon.SetActive(false);
        YellowWon.SetActive(false);
        Draw.SetActive(false);

        switch (winState)
        {
            case WinState.RedWin:
                RedWon.SetActive(true);
                break;
            case WinState.YellowWin:
                YellowWon.SetActive(true);
                break;
            case WinState.Draw:
                Draw.SetActive(true);
                break;
            case WinState.MatchNotFinished:
                break;
            default:
                break;
        }
    }
}
