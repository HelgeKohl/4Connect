using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class StateResult
{
    public int[,] State { get; set; }
    public int[,][] HoleCoords { get; set; }
    public int[][] ColCoords { get; set; }
    public int CountRedChips { get; set; }
    public int CountYellowChips { get; set; }
    public bool isValid { get; set; }
    public int MeanChipSize { get; set; }

    public StateResult()
    {
        State = new int[7, 6];
        ColCoords = new int[7][];
        HoleCoords = new int[7, 6][];
        CountRedChips = 0;
        CountYellowChips = 0;
        MeanChipSize = 0;
    }
}


