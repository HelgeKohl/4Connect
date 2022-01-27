using System.Collections;
using System.Collections.Generic;
using OpenCvSharp;
using UnityEngine;

public class StateDetection
{
    private int rows = 6;
    private int cols = 7;

    public int id_red = 1;
    public int id_yellow = -1;

    // color spaces
    // red
    private OpenCvSharp.Scalar lower_red_1;
    private OpenCvSharp.Scalar higher_red_1;
    private OpenCvSharp.Scalar lower_red_2;
    private OpenCvSharp.Scalar higher_red_2;
    // yellow
    private OpenCvSharp.Scalar lower_yellow;
    private OpenCvSharp.Scalar higher_yellow;
    // blue
    private OpenCvSharp.Scalar lower_blue;
    private OpenCvSharp.Scalar higher_blue;

    public StateDetection()
    {
        lower_red_1 = new OpenCvSharp.Scalar(0, 175, 50);
        higher_red_1 = new OpenCvSharp.Scalar(10, 255, 255);
        lower_red_2 = new OpenCvSharp.Scalar(170, 175, 50);
        higher_red_2 = new OpenCvSharp.Scalar(180, 255, 255);
        lower_yellow = new OpenCvSharp.Scalar(15, 125, 20);
        higher_yellow = new OpenCvSharp.Scalar(45, 255, 255);
        lower_blue = new OpenCvSharp.Scalar(100, 100, 20);
        higher_blue = new OpenCvSharp.Scalar(140, 255, 255);
    }

    public StateResult detectState(Mat frame)
    {
        // Image Preprocessing
        Mat preproccessed = new Mat();
        imagePreprocessing(frame, out preproccessed);

        List<Point[]> contour_list;
        List<OpenCvSharp.Rect> rect_list;
        List<int[]> position_list;

        // setup lists of holedata
        setupLists(preproccessed, out contour_list, out rect_list, out position_list);

        if (position_list.Count > 0)
        {
            getState(rect_list, position_list, contour_list, frame, out StateResult result);
            result.isValid = isValid(result);
            return result;
        }
        else
        {
            return new StateResult();
        }
    }

    // imagepreprocessing for board detection
    private void imagePreprocessing(Mat FrameIn, out Mat FrameOut)
    {
        // Frame in HSV-ColorSpace
        Mat hsv = new Mat();
        Cv2.CvtColor(FrameIn, hsv, ColorConversionCodes.BGR2HSV);

        // Mask for Board
        Mat blue_mask = new Mat();
        Cv2.InRange(hsv, lower_blue, higher_blue, blue_mask);
        hsv.Dispose();

        Mat board_only = new Mat();
        Cv2.BitwiseAnd(FrameIn, FrameIn, board_only, blue_mask);
        blue_mask.Dispose();

        // convert to grayscale
        Mat board_only_gray = new Mat();
        Cv2.CvtColor(board_only, board_only_gray, ColorConversionCodes.BGR2GRAY);
        board_only.Dispose();

        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
        Mat dilated = new Mat();
        Cv2.Dilate(board_only_gray, dilated, kernel, null, 1);
        board_only_gray.Dispose();

        Mat eroded = new Mat();
        Cv2.Erode(dilated, eroded, kernel, null, 3);
        dilated.Dispose();

        // threshold grayscale
        Mat thresh = new Mat();
        Cv2.Threshold(eroded, thresh, 10, 255, ThresholdTypes.Binary);
        eroded.Dispose();
        kernel.Dispose();

        // canny edge detection
        FrameOut = new Mat();
        Cv2.Canny(thresh, FrameOut, 175, 200);
        thresh.Dispose();
    }

    // setup list of holes
    private void setupLists(Mat preproccessed, out List<Point[]> contour_list, out List<OpenCvSharp.Rect> rect_list, out List<int[]> position_list)
    {
        contour_list = new List<Point[]>();
        rect_list = new List<OpenCvSharp.Rect>();
        position_list = new List<int[]>();

        Point[][] contours;
        HierarchyIndex[] hierarchyIndexes;

        // find holes
        Cv2.FindContours(preproccessed, out contours, out hierarchyIndexes, OpenCvSharp.RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

        // for every hole
        for (int i = 0; i < contours.Length; i++)
        {
            // setup shape of contour
            OpenCvSharp.Point[] approx = Cv2.ApproxPolyDP(contours[i], 0.01 * Cv2.ArcLength(contours[i], true), true);
            double area = Cv2.ContourArea(contours[i]);

            OpenCvSharp.Rect rect = Cv2.BoundingRect(contours[i]);
            int x_rect = rect.X;
            int y_rect = rect.Y;
            int w_rect = rect.Width;
            int h_rect = rect.Height;

            x_rect += w_rect / 2;
            y_rect += h_rect / 2;

            int area_rect = w_rect * h_rect;

            // check if contour is a really a hole
            if (
                approx.Length >= 6 &&
                approx.Length <= 20 &&
                area > 250 &&
                area_rect < ((preproccessed.Width * preproccessed.Height) / 5) &&
                w_rect >= (h_rect - 15) &&
                w_rect <= (h_rect + 15)
            )
            {
                // add hole data 
                contour_list.Add(contours[i]);
                position_list.Add(new int[] { x_rect, y_rect });
                rect_list.Add(rect);
            }

        }
    }

    // get current playstate
    private bool getState(List<OpenCvSharp.Rect> rect_list, List<int[]> position_list, List<Point[]> contour_list, Mat frame, out StateResult result)
    {
        // Frame in HSV-ColorSpace
        result = new StateResult();

        Mat hsv = new Mat();
        Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV);

        double mean_w = 0;
        double mean_h = 0;

        foreach (OpenCvSharp.Rect rect in rect_list)
        {
            mean_w += rect.Width;
            mean_h += rect.Height;
        }

        mean_w = mean_w / rect_list.Count;
        mean_h = mean_h / rect_list.Count;

        result.MeanChipSize = (int) mean_w;

        position_list.Sort((x, y) => x[0].CompareTo(y[0]));

        int max_x = position_list[position_list.Count - 1][0];
        int min_x = position_list[0][0];

        position_list.Sort((x, y) => x[1].CompareTo(y[1]));

        int max_y = position_list[position_list.Count - 1][1];
        int min_y = position_list[0][1];

        int grid_width = max_x - min_x;
        int grid_height = max_y - min_y;

        int col_spacing = (int)(grid_width / (cols - 1));
        int row_spacing = (int)(grid_height / (rows - 1));

        // red chips
        Mat mask1 = new Mat();
        Cv2.InRange(hsv, lower_red_1, higher_red_1, mask1);

        Mat mask2 = new Mat();
        Cv2.InRange(hsv, lower_red_2, higher_red_2, mask2);

        Mat mask_red = new Mat();
        mask_red = mask1 + mask2;
        mask1.Dispose();
        mask2.Dispose();

        Mat img_red = new Mat();
        Cv2.BitwiseAnd(frame, frame, img_red, mask_red);
        img_red.Dispose();

        // yellow chips
        Mat mask_yellow = new Mat();
        Cv2.InRange(hsv, lower_yellow, higher_yellow, mask_yellow);
        hsv.Dispose();

        Mat img_yellow = new Mat();
        Cv2.BitwiseAnd(frame, frame, img_yellow, mask_yellow);
        img_yellow.Dispose();

        for (int x_i = 0; x_i < cols; x_i++)
        {
            int x = (int)(min_x + x_i * col_spacing);

            for (int y_i = 0; y_i < rows; y_i++)
            {
                int y = (int)(min_y + y_i * row_spacing);
                int r = (int)((mean_h + mean_w) / 5);

                Mat img_grid_circle = Mat.Zeros(new Size(frame.Width, frame.Height), MatType.CV_8UC1);
                Cv2.Circle(img_grid_circle, x, y, r, new OpenCvSharp.Scalar(255, 255, 255), 1);

                Mat img_res_red = new Mat();
                Cv2.BitwiseAnd(img_grid_circle, img_grid_circle, img_res_red, mask_red);

                Mat img_res_yellow = new Mat();
                Cv2.BitwiseAnd(img_grid_circle, img_grid_circle, img_res_yellow, mask_yellow);

                if (Cv2.CountNonZero(img_res_red) > 0)
                {
                    result.State[x_i, rows - y_i - 1] = id_red;
                    result.CountRedChips += 1;
                }
                else if (Cv2.CountNonZero(img_res_yellow) > 0)
                {
                    result.State[x_i, rows - y_i - 1] = id_yellow;
                    result.CountYellowChips += 1;
                }

                result.HoleCoords[x_i, rows - y_i - 1] = new int[]{ x, y };

                img_grid_circle.Dispose();
                img_res_red.Dispose();
                img_res_yellow.Dispose();
            }

            result.ColCoords[x_i] = new int[] { x, (result.HoleCoords[x_i, 5][1] - (int)mean_h * 2)};
        }


        mask_red.Dispose();
        mask_yellow.Dispose();

        return true;
    }

    private bool isValid(StateResult result)
    {
        bool countDifferenceIsValid = false;
        if(result.CountRedChips == result.CountYellowChips ||  // Gleiche Anzahl
           result.CountRedChips == result.CountYellowChips + 1 // Der Rote hat einen Chip mehr, er ist Startspieler
           )
        {
            countDifferenceIsValid = true;
        }

        if (!countDifferenceIsValid)
        {
            return false;
        }

        for(int i = 0; i < 7; i++)
        {
            bool chipFound = false;
            for(int j = 0; j < 6; j++)
            {
                if(chipFound && result.State[i,j] != 0)
                {
                    return false;
                }
                chipFound = result.State[i, j] == 0;
            }
        }

        return true;
    }
}
