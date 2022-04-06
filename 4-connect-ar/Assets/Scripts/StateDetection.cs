using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        lower_red_1 = new OpenCvSharp.Scalar(0, 150, 70);
        higher_red_1 = new OpenCvSharp.Scalar(8, 255, 255);
        lower_red_2 = new OpenCvSharp.Scalar(160, 150, 70);
        higher_red_2 = new OpenCvSharp.Scalar(180, 255, 255);
        lower_yellow = new OpenCvSharp.Scalar(20, 120, 75);
        higher_yellow = new OpenCvSharp.Scalar(60, 255, 255);
        lower_blue = new OpenCvSharp.Scalar(100, 100, 80);
        higher_blue = new OpenCvSharp.Scalar(135, 255, 255);
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
            if(getState(rect_list, position_list, contour_list, frame, out StateResult result))
            {
                result.isValid = isValid(result);
                return result;
            }
            else
            {
                result.isValid = false;
                return result;
            }
        }
        else
        {
            StateResult result = new StateResult();
            result.isValid = false;
            return result;
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

        // threshold grayscale
        Mat thresh = new Mat();
        Cv2.Threshold(board_only_gray, thresh, 10, 255, ThresholdTypes.Binary);
        board_only_gray.Dispose();

        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
        Mat dilated = new Mat();
        Cv2.Dilate(thresh, dilated, kernel, null, 3);
        thresh.Dispose();

        Mat eroded = new Mat();
        Cv2.Erode(dilated, eroded, kernel, null, 1);
        dilated.Dispose();
        kernel.Dispose();

        // canny edge detection
        FrameOut = new Mat();
        Cv2.Canny(eroded, FrameOut, 175, 200);
        eroded.Dispose();
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
                if(!position_list.Exists(x => x[0] == x_rect && x[1] == y_rect))
                {
                    contour_list.Add(contours[i]);
                    position_list.Add(new int[] { x_rect, y_rect });
                    rect_list.Add(rect);
                }
            }

        }

        var unique_position_list = position_list.Distinct().ToList();
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

        if (position_list.Count == 42)
        {
            position_list.Sort((x, y) => x[0].CompareTo(y[0]));

            List<List<int[]>> sorted_position_list_tmp = new List<List<int[]>>();
            for (int i = 0; i < 7; i++)
            {
                List<int[]> row = new List<int[]>();
                for (int j = 0; j < 6; j++)
                {
                    row.Add(position_list[i * 6 + j]);
                }
                sorted_position_list_tmp.Add(row);
            }
            List<int[]> sorted_position_list = new List<int[]>();

            foreach (List<int[]> item in sorted_position_list_tmp)
            {
                item.Sort((x, y) => x[1].CompareTo(y[1]));
                foreach (int[] position in item)
                {
                    sorted_position_list.Add(position);
                }
            }

            int x_i = 0;
            int y_i = 0;
            bool isFirstRow = true;

            for (int i = 0; i < sorted_position_list.Count; i++)
            {
                int x = (int)sorted_position_list[i][0];
                int y = (int)sorted_position_list[i][1];
                int r = (int)((mean_h + mean_w) / 5);

                Mat img_grid_circle = Mat.Zeros(new Size(frame.Width, frame.Height), MatType.CV_8UC1);
                Cv2.Circle(img_grid_circle, x, y, r, new OpenCvSharp.Scalar(255, 255, 255), -1);

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

                result.HoleCoords[x_i, rows - y_i - 1] = new int[] { x, y };

                img_grid_circle.Dispose();
                img_res_red.Dispose();
                img_res_yellow.Dispose();

                if (isFirstRow)
                {
                    result.ColCoords[x_i] = new int[] { x, (result.HoleCoords[x_i, 5][1] - (int)mean_h * 2) };
                }

                isFirstRow = false;
                y_i++;
                if (y_i == 6)
                {
                    y_i = 0;
                    x_i++;
                    isFirstRow = true;
                }
            }

            mask_red.Dispose();
            mask_yellow.Dispose();

            if(result.ColCoords.Any(x => x == null))
            {
                return false;
            }

            return true;
        }
        return false;
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
