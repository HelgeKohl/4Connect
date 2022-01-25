using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WinState
{
    RedWin,
    YellowWin,
    Draw,
    MatchNotFinished
}

public enum Player
{
    Red = 1,
    Yellow = 2
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

    public BoardEvaluator Evaluator { get; set; }

    public Player CurrentPlayer = Player.Red;
    public Player StartingPlayer = Player.Red;

    public Board()
    {
        Width = 7;
        Height = 6;
        Evaluator = new BoardEvaluator(this);
        Reset();
    }

    public void Reset()
    {
        State = new int[Width, Height];
    }

    public int GetRowOfInsertedChip(int column)
    {
        for (int row = 0; row < Height; row++)
        {
            if (State[column, row] == 0)
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
        // TODO: Hovereffekt über der Spalte anzeigen??
        Debug.Log("Spalte: " + columnIndex);
        //throw new NotImplementedException();
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
            State[column, row] = 1;
        }
        else
        {
            State[column, row] = 2;
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
                int value = 0;

                if (player == Player.Red)
                {
                    value = State[x, y];
                } 
                else
                {
                    if (State[x, y] == 1)
                    {
                        value = 2;
                    }
                    else if (State[x, y] == 2)
                    {
                        value = 1;
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


}
