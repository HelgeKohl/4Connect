using OpenCvSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectDetection
{
    CascadeClassifier board_haar_cascade;
    List<OpenCvSharp.Rect> boardBounds = new List<OpenCvSharp.Rect>();

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
        //file = "Haar/cascade.xml";
        Debug.Log(file);
        board_haar_cascade = new CascadeClassifier(file);
    }

    public Mat DetectObjects(Mat image)
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
        MarkFeatures(image);

        //OpenCvHelper.Overlay = OpenCvSharp.Unity.MatToTexture(image);

        //return OpenCvHelper.Overlay;
        return image;
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

    private void MarkFeatures(Mat image)
    {
        foreach (OpenCvSharp.Rect bounds in boardBounds)
        {
            OpenCvSharp.Rect biggerRect = new OpenCvSharp.Rect();
            double scaleXby = 1.6;
            double scaleYby = 1.2;
            biggerRect.X = bounds.X - (int)(bounds.Width * ((scaleXby - 1) / 2));
            biggerRect.Y = bounds.Y - (int)(bounds.Height * ((scaleYby - 1) / 2));
            biggerRect.Width = (int)(bounds.Width * scaleXby);
            biggerRect.Height = (int)(bounds.Height * scaleYby);

            biggerRect.X = biggerRect.X < 0 ? 0 : biggerRect.X;
            biggerRect.Y = biggerRect.Y < 0 ? 0 : biggerRect.Y;
            biggerRect.Width = biggerRect.X + biggerRect.Width > Screen.width ? Screen.width : biggerRect.Width;
            biggerRect.Height = biggerRect.Y + biggerRect.Height > Screen.height ? Screen.height : biggerRect.Height;

            Cv2.Rectangle(image, bounds, new Scalar(0, 255, 0), thickness: 5);

            Cv2.Rectangle(image, biggerRect, new Scalar(255, 255, 0), thickness: 5);
        }
    }
}
