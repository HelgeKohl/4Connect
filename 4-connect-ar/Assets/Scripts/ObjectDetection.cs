using OpenCvSharp;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum BoardOperations
{
    Highlight,
    CropInnerRegion,
    CropOuterRegion
}
public class ObjectDetection
{
    CascadeClassifier board_haar_cascade;
    List<OpenCvSharp.Rect> boardBounds = new List<OpenCvSharp.Rect>();
    public OpenCvSharp.Rect BoardRegionBounds;

    public ObjectDetection()
    {
        //Define the face and eyes classifies using Haar-cascade xml
        //Download location: https://github.com/opencv/opencv/tree/master/data/haarcascades
        //
        // TODO: Dafür wird noch eine Lösung gebraucht ...
        //board_haar_cascade = new CascadeClassifier(@"C:\Development\arvr-projekt-meta\data\images\cascade\cascade.xml");
        //board_haar_cascade = new CascadeClassifier(@"http://pastebin.com/raw/UavKbfwm");
        //board_haar_cascade = new CascadeClassifier(Application.dataPath + @"/StreamingAssets/Haar/cascade.xml");

        string file = Application.streamingAssetsPath + @"/Haar/cascade.xml";

        if (Application.platform == RuntimePlatform.Android)
        {
            WWW www = new WWW(file);
            while (!www.isDone) { }
            string persistantPath = Application.persistentDataPath + @"/Haar/cascade.xml";
            File.WriteAllBytes(persistantPath, www.bytes);
            file = persistantPath;
        }
        Debug.Log(file);
        board_haar_cascade = new CascadeClassifier(file);
    }

    public Mat DetectObjects(Mat image, BoardOperations operation = BoardOperations.CropOuterRegion)
    {
        if (image == null)
        {
            return null;
        }

        // Convert to gray scale to improve the image processing
        Mat gray = ConvertGrayScale(image);

        // Detect boards using Cascase classifier
        OpenCvSharp.Rect[] boards = DetectBoards(gray);
        if (image.Empty())
            return null;

        // Alte Position löschen
        boardBounds.Clear();

        // Loop through detected boards
        foreach (var item in boards)
        {
            boardBounds.Add(item);
        }

        // Mark the detected board on the original frame
        Mat result = MarkFeatures(image, operation);

        return result;
    }

    private Mat ConvertGrayScale(Mat image)
    {
        Mat gray = new Mat();
        Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
        return gray;
    }

    private OpenCvSharp.Rect[] DetectBoards(Mat image)
    {
        // Parameter zum Anpassen
        OpenCvSharp.Rect[] boardFeature = board_haar_cascade.DetectMultiScale(image, 1.3, 50);
        return boardFeature;
    }

    private Mat MarkFeatures(Mat image, BoardOperations operation)
    {
        foreach (OpenCvSharp.Rect bounds in boardBounds)
        {
            // Scaling
            OpenCvSharp.Rect biggerRect = new OpenCvSharp.Rect();
            double scaleXby = 1.8; // X-Faktor Skalierung
            double scaleYby = 1.4; // Y-Faktor Skalierung
            biggerRect.X = bounds.X - (int)(bounds.Width * ((scaleXby - 1) / 2));
            biggerRect.Y = bounds.Y - (int)(bounds.Height * ((scaleYby - 1) / 2));
            int scaledWith = (int)(bounds.Width * scaleXby);
            int scaledHeight = (int)(bounds.Height * scaleYby);
            biggerRect.X = biggerRect.X < 0 ? 0 : biggerRect.X;
            biggerRect.Y = biggerRect.Y < 0 ? 0 : biggerRect.Y;
            bool isWidthAboveScreenWidth = biggerRect.X + scaledWith > image.Width;
            bool isHeightAboveScreenHeight = biggerRect.Y + scaledHeight > image.Height;
            int maxScreenWidth = image.Width - biggerRect.X;
            int maxScreenHeight = image.Height - biggerRect.Y;
            biggerRect.Width = isWidthAboveScreenWidth ? maxScreenWidth : scaledWith;
            biggerRect.Height = isHeightAboveScreenHeight ? maxScreenHeight : scaledHeight;

            if (biggerRect.Width <= 0)
            {
                biggerRect.Width = scaledWith;
            }
            if (biggerRect.Height <= 0)
            {
                biggerRect.Height = scaledHeight;
            }

            // Operation durchführen
            if (operation == BoardOperations.Highlight)
            {
                // Tatsächlich gefundenes Rect
                Cv2.Rectangle(image, bounds, new Scalar(0, 255, 0), thickness: 5);
                // Rect mit Pufferzone
                Cv2.Rectangle(image, biggerRect, new Scalar(255, 255, 0), thickness: 5);

                BoardRegionBounds = image.BoundingRect();
                return image;
            }
            else if (operation == BoardOperations.CropInnerRegion)
            {
                BoardRegionBounds = bounds;
                return new Mat(image, bounds);
            }
            else if (operation == BoardOperations.CropOuterRegion)
            {
                BoardRegionBounds = biggerRect;
                return new Mat(image, biggerRect);
            }
        }

        return null;
    }
}
