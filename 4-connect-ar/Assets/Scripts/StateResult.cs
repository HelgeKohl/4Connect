using System;
using OpenCvSharp;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class StateResult
{
    public int[,] State { get; set; }
    public int[,][] HoleCoords { get; set; }
    public int[][] ColCoords { get; set; }
    public int CountRedChips { get; set; }
    public int CountYellowChips { get; set; }
    public bool isValid { get; set; }
    public int MeanChipSize { get; set; }
    public Mat Frame { get; set; }
    public int boardX { get; set; }
    public int boardY { get; set; }
    public int HolesFound { get; set; } = 0;

    public StateResult()
    {
        State = new int[7, 6];
        ColCoords = new int[7][];
        HoleCoords = new int[7, 6][];
        CountRedChips = 0;
        CountYellowChips = 0;
        MeanChipSize = 0;
        Frame = new Mat();
    }
}


