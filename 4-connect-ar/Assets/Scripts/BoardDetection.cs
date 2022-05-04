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
    public int Width = 640;
    public int Height = 480;
    public BaseAgent Agent;
    public RectTransform CanvasRectTransform;

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
    private int suggestedIndex = -1;

    // WinStates
    public GameObject YellowWon;
    public GameObject RedWon;
    public GameObject Draw;

    // Debug
    public bool debug;
    public bool debugDetection;
    public bool debugFps;
    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    bool isCameraInitialized = false;

    void Start()
    {
        // https://www.nuget.org/packages/OpenCvSharp4/
        // https://www.tech-quantum.com/have-fun-with-webcam-and-opencv-in-csharp-part-1/
        // https://www.tech-quantum.com/have-fun-with-webcam-and-opencv-in-csharp-part-2/

        if (debugDetection)
        {
            stateDetection = new StateDetection(debugDetection);
        }
        else
        {
            stateDetection = new StateDetection();
        }
        
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

    internal Mat GetNaoImageFrameAsMat()
    {
        if (NaoSocketServer.ImageBytes == null)
        {
            return null;
        }

        Texture2D image2d = new Texture2D(640, 480);
        image2d.LoadImage(NaoSocketServer.ImageBytes);
        Mat matrix = OpenCvSharp.Unity.TextureToMat(image2d);
        Destroy(image2d);

        return matrix;
    }

    internal Texture2D GetNaoImageFrameAsTexture2D()
    {
        if (NaoSocketServer.ImageBytes == null)
        {
            return null;
        }

        if (NaoSocketServer.CurrentTexture2D == null)
        {
            Destroy(NaoSocketServer.CurrentTexture2D);
            NaoSocketServer.CurrentTexture2D = new Texture2D(640, 480, TextureFormat.BGRA32, false);
            NaoSocketServer.CurrentTexture2D.LoadImage(NaoSocketServer.ImageBytes);
        }

        return NaoSocketServer.CurrentTexture2D;
    }

    private void FixedUpdate()
    {
        bool useCameraInput = false;

        if (!isCameraInitialized)
        {
            Screen.SetResolution(Width, Height, FullScreenMode.Windowed);
            camera = new CustomCamera(background, Width, Height);
            isCameraInitialized = true;
        }
        if (!camera.IsCameraAvailable() && useCameraInput)
        {
            return;
        }

        // Time
        stopwatch.Restart();

        if (useCameraInput)
        {
            camera.Refresh();
            threadInputMat = camera.GetCurrentFrameAsMat();
        }
        else
        {
            // Nao Magic
            Texture2D texture2d = GetNaoImageFrameAsTexture2D();
            camera.SetCustomTexture(texture2d);
            threadInputMat = camera.GetCurrentFrameAsMat();
        }
        
        if (threadResponseStateResult != null && threadInputMat != null && threadResponseStateResult.isValid)
        {
            for (int i = 0; i < threadResponseStateResult.ColCoords.Length; i++)
            {
                int[] item = threadResponseStateResult.ColCoords[i];
                OpenCvSharp.Rect bounds = new OpenCvSharp.Rect();

                try
                {
                    bounds.X = item[0];
                    bounds.Y = item[1];
                }
                catch (Exception)
                {
                    threadResponseMat = threadInputMat;
                    return;
                }

                

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
                    
                    // Stelle Chip an richtiger Position dar, Größe wie Original
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

                    // Füge Chip ein
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
        else
        {
            threadResponseMat = threadInputMat;
        }

        if (threadInputMat != null)
        {
            //Debug.Log("Do");
            //chipImage.CopyTo(threadInputMat.RowRange(0, 63).ColRange(0, 63));
            
        }

        TryAddCurrentMat();

        if (OpenCvHelper.Overlay != null)
        {
            if (this.debugDetection)
            {
                background.texture = OpenCvSharp.Unity.MatToTexture(threadResponseStateResult.Frame);
            }
            else
            {
                background.texture = OpenCvHelper.Overlay;
            }
        }

        ShowWinState(board.WinState);

        if (board.WinState == WinState.MatchNotFinished && suggestedIndex >= 0)
        {
            ShowSuggestedPiece(suggestedIndex);
        }

        // Response to Nao
        //if (threadResponseStateResult != null && threadResponseStateResult.isValid)
        //if (threadResponseStateResult != null)
        //{
        if (NaoSocketServer.NaoRequestActive 
            && threadResponseStateResult != null
            && threadResponseStateResult.isValid
            && suggestedIndex != -1
            && NaoSocketServer.NaoRequestFinished)
        {
            NaoSocketServer.SetState(board.WinState, suggestedIndex);
            NaoSocketServer.ImageBytes = null;
            threadInputMat = null;
            NaoSocketServer.NaoRequestActive = false;
        }
            //threadInputMat = null;
        //}

        // ---

        stopwatch.Stop();
        if (debugFps && stopwatch.ElapsedMilliseconds != 0)
        {
            Debug.Log(stopwatch.ElapsedMilliseconds);
        }

        if (debugDetection)
        {
            Resources.UnloadUnusedAssets();
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
                if (camera == null || threadInputMat == null || threadResponseMat != null || Agent.Board == null || NaoSocketServer.NaoRequestFinished)
                {
                    continue;
                }

                // TODO: bei detectState statt mat nur noch das Teil-Rect aus DetectObjects übergeben
                StateResult result = stateDetection.detectState(threadInputMat);

                int boardX = result.boardX;
                int boardY = result.boardY;

                if (result.isValid)
                {
                    foreach (var item in result.ColCoords)
                    {
                        item[0] += boardX;
                        item[1] += boardY;
                    }
                }

                threadResponseStateResult = result;

                // Prüfe ob State sich geändert hat

                bool gridStateHasChanged = true;
                bool gridStateChangedRight = true;
                if (suggestedIndex >= 0)
                {
                    // Aktuelles Spielfeld ist anders als das davor, egal, ob mehr oder weniger Chips sind?
                    gridStateHasChanged = stateChanged(result.State, board.State);
                    // Wir prüfen, ob an dem davor vorgeschlagenen Spielfeldslot der gewünschte Chip eingeworfen wurde
                    gridStateChangedRight = checkSuggestedCoin(result.State);
                }
                

                if (!result.isValid)
                {
                    //board.Reset();
                    //board.UpdateWinstate();
                }
                else
                {
                    if (!gridStateHasChanged)
                    {
                        // @Nao: Es hat sich nichts geändert
                        board.WinState = WinState.NoChange;
                        Debug.Log("Es hat sich nichts geändert");
                    }
                    if (!gridStateChangedRight && gridStateHasChanged)
                    {
                        // @Nao: Der Benutzer hat den gelben Chip nicht an der vom Nao gewünschten Spalte eingeworfen
                        board.WinState = WinState.Wrong;
                        Debug.Log("Der Benutzer hat den gelben Chip nicht an der vom Nao gewünschten Spalte eingeworfen");
                    }
                    // aktualisieren des States
                    //board.State = FlipArrayHorizontal(result.State);
                    board.State = result.State;

                    if (gridStateHasChanged && result.isValid && gridStateChangedRight)
                    {
                        board.CurrentPlayer = result.CountRedChips > result.CountYellowChips ? Player.Yellow : Player.Red;

                        bool gewinneIchMitDiesemZug = false;
                        if (result.CountRedChips + result.CountYellowChips < 42)
                        {
                            Debug.Log("Nao is thinking ...");

                            Agent.IsThinking = true;
                            Agent.RequestDecision();
                            while (Agent.IsThinking)
                            {
                                Debug.Log("Still sinking ...");
                                System.Threading.Thread.Sleep(10);
                            }

                            // Prüfe, ob der Nao mit dem nächsten Zug gewinnen würde
                            Board boardSimulateCheckNextTurn = new Board();
                            boardSimulateCheckNextTurn.State = board.State.Clone() as int[,];
                            boardSimulateCheckNextTurn.CurrentPlayer = board.CurrentPlayer;
                            boardSimulateCheckNextTurn.emptyChip = board.emptyChip;
                            boardSimulateCheckNextTurn.redChip = board.redChip;
                            boardSimulateCheckNextTurn.yellowChip = board.yellowChip;
                            boardSimulateCheckNextTurn.InsertChip(suggestedIndex);
                            boardSimulateCheckNextTurn.UpdateWinstate();

                            gewinneIchMitDiesemZug = boardSimulateCheckNextTurn.WinState == WinState.YellowWin;
                        }
                        board.UpdateWinstate();
                        if (gewinneIchMitDiesemZug)
                        {
                            board.WinState = WinState.YellowWin;
                        }
                    }

                }

                // Was soll angezeigt werden

                if (this.debug)
                {
                    board.printGrid();
                }

                NaoSocketServer.NaoRequestFinished = true;
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

    private bool checkSuggestedCoin(int[,] inputGrid)
    {
        if(suggestedIndex < 0)
        {
            return true;
        }

        Debug.Log("Akt. Suggested Index: " + NaoSocketServer.SuggestedIndex);

        string grid_str = "";
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                grid_str += inputGrid[j, i] + "\t";
            }
            grid_str += "\n";
        }
        Debug.Log("Akt. State: \n" + grid_str);

        string grid_str_2 = "";
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                grid_str_2 += board.State[j, i] + "\t";
            }
            grid_str_2 += "\n";
        }
        Debug.Log("Letzter State: \n" + grid_str_2);

        for (int i = 0; i < 6; i++)
        {
            if (board.State[NaoSocketServer.SuggestedIndex, i] == 0)
            {
                if(inputGrid[NaoSocketServer.SuggestedIndex, i] == -1)
                {
                    return true;
                }
                else
                {
                    Debug.Log("checkSuggestedCoin");
                    Debug.Log(board.State[NaoSocketServer.SuggestedIndex, i]);
                    Debug.Log(inputGrid[NaoSocketServer.SuggestedIndex, i]);
                    return false;
                }
            }
        }
        Debug.Log("hier sollten wir garnicht landen");
        return false;
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
            case WinState.NoChange:
                break;
            case WinState.Wrong:
                break;
            default:
                break;
        }
    }
}
