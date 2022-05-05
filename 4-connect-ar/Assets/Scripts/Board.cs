using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WinState
{
    NoChange = 0,
    Wrong = 1,
    YellowWin = 2,
    RedWin = 3,
    MatchNotFinished = 4,
    Draw = 5,
    AmountOfHolesIsWrong = 6,
}

public enum Player
{
    //Red = 1,
    //Yellow = 2
    Red = 1,
    Yellow = -1
}

public class Board
{
    public int Width { get; set; }
    public int Height { get; set; }

    // 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0
    // 0 0 0 0 0 0 0
    // 0 0 0 1 0 0 0
    // 0 0 0 2 0 0 0
    // 0 0 0 1 2 1 0
    public int[,] State { get; set; } // is empty, 1 is player1, 2 is player2
    public int[,] OldState { get; set; }

    public BoardEvaluator Evaluator { get; set; }
    public WinState WinState { get; set; }

    // Es muss ebenso public enum Player gesetzt werden ...
    public int emptyChip = 0; // KOMMENTAR Ansehen!
    public int redChip = 1; // KOMMENTAR Ansehen!
    public int yellowChip = 2; // KOMMENTAR Ansehen!

    public Player CurrentPlayer = Player.Red;
    public Player StartingPlayer = Player.Red;
    private BoardDetection boardDetection;
    

    public Board()
    {
        Width = 7;
        Height = 6;
        Evaluator = new BoardEvaluator(this);
        Reset();
    }

    public Board(BoardDetection boardDetection) : this()
    {
        this.boardDetection = boardDetection;
    }

    public void UpdateWinstate()
    {
        WinState = Evaluator.Evaluate();
    }

    public void Reset()
    {
        State = new int[Width, Height];
        OldState = new int[Width, Height];
    }

    public int GetRowOfInsertedChip(int column)
    {
        for (int row = 0; row < Height; row++)
        {
            if (State[column, row] == emptyChip)
            {
                return row;
            }
        }

        return -1;
    }

    /// <summary>
    /// Spalte, in die der Agent seinen Chip werfen möchte
    /// </summary>
    /// <param name="columnIndex">Spaltenindex</param>
    /// <param name="player">Welcher Spieler es ist</param>
    public void SelectColumn(int columnIndex, Player player)
    {
        boardDetection?.SuggestColumn(columnIndex);
    }

    /// <summary>
    /// Gibt zurück, in welcher Reihe der Chip landet.
    /// -1 => Spalte ist bereits voll.
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    public int InsertChip(int column)
    {
        int row = GetRowOfInsertedChip(column);

        if (CurrentPlayer == Player.Red)
        {
            State[column, row] = redChip;
        }
        else
        {
            State[column, row] = yellowChip;
        }

        return row;
    }

    internal IEnumerable<int> GetAvailableColumns()
    {
        List<int> columns = new List<int>();
        for (int i = 0; i < Width; i++)
        {
            if (Evaluator.IsColumnAvailable(i))
            {
                columns.Add(i);
            }
        }

        return columns;
    }

    internal IEnumerable<int> GetBoardStateAs1DArray(Player player)
    {
        List<int> values = new List<int>();

        for (int x = 0; x < Width - 3; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                int value = emptyChip;

                if (player == Player.Red)
                {
                    value = State[x, y];
                } 
                else
                {
                    if (State[x, y] == redChip)
                    {
                        value = yellowChip;
                    }
                    else if (State[x, y] == yellowChip)
                    {
                        value = redChip;
                    }
                }

                values.Add(value);
            }
        }

        return values;
    }

    internal IEnumerable<int> GetOccupiedColumns()
    {
        List<int> columns = new List<int>();
        for (int i = 0; i < Width; i++)
        {
            if (!Evaluator.IsColumnAvailable(i))
            {
                columns.Add(i);
            }
        }

        return columns;
    }

    public void printGrid()
    {
        string grid_str = "";
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                grid_str += State[j, i] + "\t";
            }
            grid_str += "\n";
        }
        Debug.Log(grid_str);
    }
}
