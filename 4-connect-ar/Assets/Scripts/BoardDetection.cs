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
    public RectTransform CanvasRectTransform;

    private ObjectDetection objectDetection;
    private StateDetection stateDetection;
    private Board board;
    private new CustomCamera camera;
    private Thread cv2WorkerThread;
    private readonly object lockObj = new object();
    private Mat threadResponseMat;
    private StateResult threadResponseStateResult;
    private Mat threadInputMat;

    // Chip Image
    private Mat chipImageRed;
    private Mat chipImageYellow;

    // Position Hover-Column
    public GameObject RedPiece;
    public GameObject YellowPiece;
    private float posX;
    private float posY;
    private int suggestedIndex = -1;

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
        board = new Board(this);
        board.redChip = stateDetection.id_red;
        board.yellowChip = stateDetection.id_yellow;
        board.UpdateWinstate();
        Agent.Board = board;


        Texture.allowThreadedTextureCreation = true;


        // Chips zum Einwerfen
        Texture2D textureRed = RedPiece.GetComponent<Image>().mainTexture as Texture2D;
        Texture2D textureYellow = YellowPiece.GetComponent<Image>().mainTexture as Texture2D;
        chipImageRed = OpenCvSharp.Unity.TextureToMat(textureRed);
        chipImageYellow = OpenCvSharp.Unity.TextureToMat(textureYellow);
        Cv2.Resize(chipImageRed, chipImageRed, new Size(64, 64));
        Cv2.Resize(chipImageYellow, chipImageYellow, new Size(64, 64));
    }

    internal void SuggestColumn(int columnIndex)
    {
        this.suggestedIndex = columnIndex;
    }

    private void FixedUpdate()
    {
        // Time
        stopwatch.Restart();

        camera.Refresh();
        threadInputMat = camera.GetCurrentFrameAsMat();

        
        if (threadResponseStateResult != null && threadInputMat != null && threadResponseStateResult.isValid)
        {
            for (int i = 0; i < threadResponseStateResult.ColCoords.Length; i++)
            {
                int[] item = threadResponseStateResult.ColCoords[i];
                OpenCvSharp.Rect bounds = new OpenCvSharp.Rect();
                bounds.X = item[0];
                bounds.Y = item[1];

                

                bounds.Width = 2;
                bounds.Height = 2;
                Scalar color;
                if (i == suggestedIndex)
                {
                    color = new Scalar(0, 255, 0);

                    Mat chipImage = board.CurrentPlayer == Player.Red ? chipImageRed : chipImageYellow;
                    int desiredLength = threadResponseStateResult.MeanChipSize;
                    Mat resizedChip = new Mat();
                    Cv2.Resize(chipImage, resizedChip, new Size(desiredLength, desiredLength));

                    int leftPosX = bounds.X - resizedChip.Width / 2;
                    int topPosY = bounds.Y - resizedChip.Height / 2;
                    int rightPosX = leftPosX + resizedChip.Width;
                    int BottomPosY = topPosY + resizedChip.Height;

                    if(leftPosX < 0 || topPosY < 0 || leftPosX > threadInputMat.Width || topPosY > threadInputMat.Height)
                    {
                        continue;
                    }
                    
                    // Stelle Chip an richtiger Position dar, Gr��e wie Original
                    Mat chip_only = Mat.Zeros(new Size(threadInputMat.Width, threadInputMat.Height), MatType.CV_8UC3);
                    resizedChip.CopyTo(chip_only.ColRange(leftPosX, rightPosX).RowRange(topPosY, BottomPosY));

                    // Grayscale zum Thresholdne
                    Mat chip_grayscale = new Mat();
                    Cv2.CvtColor(chip_only, chip_grayscale, ColorConversionCodes.BGR2GRAY);

                    // erstellen der Chip Maske
                    Mat chip_mask = new Mat();
                    Cv2.Threshold(chip_grayscale, chip_mask, 5, 255, ThresholdTypes.Binary);
                    chip_grayscale.Dispose();

                    Cv2.CvtColor(chip_mask, chip_mask, ColorConversionCodes.GRAY2BGR);

                    // Setze Pixel an der Stelle, an der der Chip ist, auf schwarz
                    threadInputMat = threadInputMat - chip_mask;
                    chip_mask.Dispose();

                    // F�ge Chip ein
                    threadInputMat = threadInputMat + chip_only;
                    chip_only.Dispose();
                    Scalar border_color = board.CurrentPlayer == Player.Red ? new Scalar(4, 4, 143) : new Scalar(74, 255, 251);
                    Cv2.Circle(threadInputMat, bounds.X, bounds.Y, resizedChip.Width/2, border_color, 2);
                    resizedChip.Dispose();
                }
                else
                {
                    color = new Scalar(255, 0, 0);
                    Cv2.Rectangle(threadInputMat, bounds, color, thickness: 5);
                }

                
                threadResponseMat = threadInputMat;
            }
        }

        if (threadInputMat != null)
        {
            //Debug.Log("Do");
            //chipImage.CopyTo(threadInputMat.RowRange(0, 63).ColRange(0, 63));
            
        }

        TryAddCurrentMat();

        if (OpenCvHelper.Overlay != null)
        {
            background.texture = OpenCvHelper.Overlay;
        }

        ShowWinState(board.WinState);
        ShowSuggestedPiece(suggestedIndex);
        if (board.WinState == WinState.MatchNotFinished && suggestedIndex >= 0)
        {
            ShowSuggestedPiece(suggestedIndex);
        }
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

    private void TryAddCurrentMat()
    {
        lock (lockObj)
        {
            if (threadResponseMat == null)
            {
                return;
            }

            OpenCvHelper.Overlay = OpenCvSharp.Unity.MatToTexture(threadResponseMat);
            threadResponseMat = null;
        }
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

                // TODO: bei detectState statt mat nur noch das Teil-Rect aus DetectObjects �bergeben
                StateResult result = stateDetection.detectState(boardRegion == null || regionSelectionOperation == BoardOperations.Highlight ? threadInputMat : boardRegion);

                int boardX = objectDetection.BoardRegionBounds.X;
                int boardY = objectDetection.BoardRegionBounds.Y;

                foreach (var item in result.ColCoords)
                {
                    if(item != null && item.Length == 2)
                    {
                        item[0] += boardX;
                        item[1] += boardY;
                    }
                }

                threadResponseStateResult = result;

                // Pr�fe ob State sich ge�ndert hat
                bool gridStateHasChanged = stateChanged(result.State, board.State);

                if (!result.isValid)
                {
                    //board.Reset();
                    //board.UpdateWinstate();
                }
                else
                {
                    // aktualisieren des States
                    //board.State = FlipArrayHorizontal(result.State);
                    board.State = result.State;

                    if (gridStateHasChanged && result.isValid)
                    {
                        board.CurrentPlayer = result.CountRedChips > result.CountYellowChips ? Player.Yellow : Player.Red;
                        if (result.CountRedChips + result.CountYellowChips < 42)
                        {
                            Agent.RequestDecision();
                        } 
                        board.UpdateWinstate();
                    }

                    //threadResponseMat = boardRegion;
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

    public void ShowSuggestedPiece(int columnIndex)
    {
        RedPiece.SetActive(false);
        YellowPiece.SetActive(false);
        if (threadResponseStateResult != null && threadResponseStateResult.isValid)
        {
            int[] coordinates = threadResponseStateResult.ColCoords[columnIndex];
            int x = coordinates[0];
            int y = coordinates[1];
            //int x = 270;
            //int y = 90;

            GameObject piece = board.CurrentPlayer == Player.Red ? RedPiece : YellowPiece;
            piece.SetActive(false);

            piece.transform.position = new Vector2(x, y);
        }
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
