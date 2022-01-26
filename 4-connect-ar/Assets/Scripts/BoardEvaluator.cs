using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardEvaluator// : MonoBehaviour
{
    Board board;

    // Major Events
    public const int POINTS_WIN = 1000;
    public const int POINTS_LOSS = -1000;
    public const int POINTS_DRAW_STARTING_PLAYER = -200;
    public const int POINTS_DRAW_SECOND_PLAYER = 800;
           
    // Blocks
    public const int POINTS_BLOCK_4_IN_A_ROW = 700;
    public const int POINTS_BLOCK_3_IN_A_ROW = 200;
    public const int POINTS_BLOCK_3_IN_A_ROW_VERTICAL = 50;
           
    // Threat
    public const int POINTS_THREAT_3_IN_A_ROW = 70;
    public const int POINTS_THREAT_2_IN_A_ROW = 35;
    public const int POINTS_THREAT_3_IN_A_ROW_VERTICAL = 35;
    public const int POINTS_THREAT_2_IN_A_ROW_VERTICAL = 20;

    public BoardEvaluator(Board board)
    {
        this.board = board;
    }

    /// <summary>
    /// Hat der Spieler gewonnen?
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public bool DidPlayerWin(Player player)
    {
        (int, int)[] winningFields;
        return DidPlayerWin(player, out winningFields);
    }

    /// <summary>
    /// Hat der Spieler gewonnen?
    /// Was waren seine Felder, mit denen
    /// er gewonnen hat?
    /// </summary>
    /// <param name="player"></param>
    /// <param name="winningFields"></param>
    /// <returns></returns>
    public bool DidPlayerWin(Player player, out (int, int)[] winningFields)
    {
        WinState winState = Evaluate(out winningFields);

        if (player == Player.Red && winState == WinState.RedWin)
        {
            return true;
        }
        else if (player == Player.Yellow && winState == WinState.YellowWin)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Unentschieden?
    /// </summary>
    /// <returns></returns>
    public bool IsDraw()
    {
        for (int x = 0; x < board.Width; x++)
        {
            if (IsColumnAvailable(x))
            {
                return false;
            }
        }

        // Es kann in keine Spalte ein Chip gelegt werden.
        board.printGrid();
        return true;
    }

    /// <summary>
    /// Kann ein Chip in die Spalte geworfen werden?
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool IsColumnAvailable(int index)
    {
        if (board.State[index, board.Height - 1] == board.emptyChip)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Prüfe den aktuellen Status des Spielfeldes
    /// </summary>
    /// <returns></returns>
    public WinState Evaluate()
    {
        (int, int)[] winningFields;
        return Evaluate(out winningFields);
    }

    /// <summary>
    /// Prüfe den aktuellen Status des Spielfeldes
    /// Was waren seine Felder, mit denen
    /// er gewonnen hat?
    /// </summary>
    /// <returns></returns>
    public WinState Evaluate(out (int, int)[] winningFields)
    {
        WinState winState;

        // Hat Rot gewonnen?
        winState = EvaluatePlayer(Player.Red, out winningFields);
        if (winState == WinState.Draw || winState == WinState.RedWin)
        {
            // Gelb muss nicht geprüft werden, da
            // es ein Unentschieden ist oder Rot gewonnen hat.
            return winState;
        }


        // Gelb hat gewonnen oder das Spiel läuft noch.
        return EvaluatePlayer(Player.Yellow, out winningFields);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="player">Zu prüfender Spieler</param>
    /// <param name="winningFields">Felder, mit denen der Spieler gewonnen hat</param>
    /// <returns>Welchen Zustand hat das Board, nachdem geprüft wurde, ob Spieler X gewonnen hat</returns>
    private WinState EvaluatePlayer(Player player, out (int, int)[] winningFields)
    {
        WinState winState;

        if (IsDraw())
        {
            winState = WinState.Draw;
            winningFields = new (int, int)[4];
            return winState;
        }

        winState = EvaluateHorizontal(player, out winningFields);
        if (winState != WinState.MatchNotFinished)
        {
            return winState;
        }

        winState = EvaluateVertical(player, out winningFields);
        if (winState != WinState.MatchNotFinished)
        {
            return winState;
        }

        winState = EvaluateDiagonal(player, out winningFields);
        if (winState != WinState.MatchNotFinished)
        {
            return winState;
        }
        
        return winState;
    }

    private WinState EvaluateHorizontal(Player player, out (int, int)[] winningFields)
    {
        winningFields = new (int, int)[4];
        for (int x = 0; x < board.Width - 3; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (board.State[x, y] == (int)player
                    && board.State[x + 1, y] == (int)player
                    && board.State[x + 2, y] == (int)player
                    && board.State[x + 3, y] == (int)player)
                {
                    winningFields[0] = (x, y);
                    winningFields[1] = (x + 1, y);
                    winningFields[2] = (x + 2, y);
                    winningFields[3] = (x + 3, y);
                    return player == Player.Red ? WinState.RedWin : WinState.YellowWin;
                }
            }
        }

        return WinState.MatchNotFinished;
    }

    private WinState EvaluateVertical(Player player, out (int, int)[] winningFields)
    {
        winningFields = new (int, int)[4];
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height - 3; y++)
            {
                if (board.State[x, y] == (int)player
                    && board.State[x, y + 1] == (int)player
                    && board.State[x, y + 2] == (int)player
                    && board.State[x, y + 3] == (int)player)
                {
                    winningFields[0] = (x, y);
                    winningFields[1] = (x, y + 1);
                    winningFields[2] = (x, y + 2);
                    winningFields[3] = (x, y + 3);
                    return player == Player.Red ? WinState.RedWin : WinState.YellowWin;
                }
            }
        }

        return WinState.MatchNotFinished;
    }

    private WinState EvaluateDiagonal(Player player, out (int, int)[] winningFields)
    {
        winningFields = new (int, int)[4];

        // Diagonal - links unten nach rechts oben
        for (int x = 0; x < board.Width - 3; x++)
        {
            for (int y = 0; y < board.Height - 3; y++)
            {
                if (board.State[x, y] == (int)player
                    && board.State[x + 1, y + 1] == (int)player
                    && board.State[x + 2, y + 2] == (int)player
                    && board.State[x + 3, y + 3] == (int)player)
                {
                    winningFields[0] = (x, y);
                    winningFields[1] = (x + 1, y + 1);
                    winningFields[2] = (x + 2, y + 2);
                    winningFields[3] = (x + 3, y + 3);
                    return player == Player.Red ? WinState.RedWin : WinState.YellowWin;
                }
            }
        }

        // Diagonal - links oben nach rechts unten
        for (int x = 0; x < board.Width - 3; x++)
        {
            for (int y = 0; y < board.Height - 3; y++)
            {
                if (board.State[x, y + 3] == (int)player
                    && board.State[x + 1, y + 2] == (int)player
                    && board.State[x + 2, y + 1] == (int)player
                    && board.State[x + 3, y] == (int)player)
                {
                    winningFields[0] = (x, y + 3);
                    winningFields[1] = (x + 1, y + 2);
                    winningFields[2] = (x + 2, y + 1);
                    winningFields[3] = (x + 3, y);
                    return player == Player.Red ? WinState.RedWin : WinState.YellowWin;
                }
            }
        }

        return WinState.MatchNotFinished;
    }

    public int EvaluateReward(int column, int row)
    {
        int chip = board.State[column, row];
        int rewardMajorEvents = EvaluateMajorEvents(column, row, chip);
        int rewardBlocks = EvaluateBlocks(column, row, chip);
        int rewardThreats = EvaluateCreatedThreats(column, row, chip);

        return rewardMajorEvents + rewardBlocks + rewardThreats;
    }

    private int EvaluateMajorEvents(int column, int row, int chip)
    {
        int reward = 0;

        WinState winState = board.Evaluator.Evaluate();

        // Sieg durch Chip
        if (winState == WinState.RedWin && board.CurrentPlayer == Player.Red)
        {
            reward += POINTS_WIN;
        }
        else if (winState == WinState.YellowWin && board.CurrentPlayer == Player.Yellow)
        {
            reward += POINTS_WIN;
        }
        else if (winState == WinState.Draw)
        {
            if (board.StartingPlayer == board.CurrentPlayer)
            {
                // Der Startspieler hat einen Vorteil
                // Wenn er "nur" ein Untenschieden erreicht, ist das schlecht.
                reward += POINTS_DRAW_STARTING_PLAYER;
            }
            else
            {
                // Als zweiter Spieler ein Unentschieden zu erreichen,
                // ist gut.
                reward += POINTS_DRAW_SECOND_PLAYER;
            }
        }
        else if (winState == WinState.RedWin && board.CurrentPlayer == Player.Yellow
            || winState == WinState.YellowWin && board.CurrentPlayer == Player.Red)
        {
            throw new Exception("Durch das einwerfen eines Chips sollte der andere Spieler nie gewinnen ...");
        }

        return reward;
    }

    private int EvaluateBlocks(int column, int row, int chip)
    {
        int reward = 0;

        // Blocked Left
        if (BlockNThreatLeft(column, row, chip, 0, 3, 4))
        {
            reward += POINTS_BLOCK_4_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatToLeft: " + 3);
        }
        else if (BlockNThreatLeft(column, row, chip, 1, 4, 3))
        {
            reward += POINTS_BLOCK_3_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatToLeft: " + 2);
        }

        // Blocked Right
        if (BlockNThreatRight(column, row, chip, 3, 6, 4))
        {
            reward += POINTS_BLOCK_4_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatToRight: " + 3);
        }
        else if (BlockNThreatRight(column, row, chip, 2, 5, 3))
        {
            reward += POINTS_BLOCK_3_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatToRight: " + 2);
        }

        // Blocked Vertical
        if (BlockNThreatVertical(column, row, chip, 3, 6, 4))
        {
            reward += POINTS_BLOCK_4_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatVertical: " + 3);
        }
        else if (BlockNThreatVertical(column, row, chip, 2, 5, 3))
        {
            reward += POINTS_BLOCK_3_IN_A_ROW_VERTICAL;
            //Debug.LogWarning("Blocked IsThreatVertical: " + 2);
        }

        // Blocked diagonal erstellt
        // Links unten nach links oben
        if (BlockNThreat_LeftBottom_TopRight(column, row, chip, 3, 3, 6, 5, 4))
        {
            reward += POINTS_BLOCK_4_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatDiagonal LeftBottom_TopRight: " + 3);
        }
        else if (BlockNThreat_LeftBottom_TopRight(column, row, chip, 2, 2, 5, 4, 3))
        {
            reward += POINTS_BLOCK_3_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatDiagonal LeftBottom_TopRight: " + 2);
        }

        // Rechts unten nach links oben
        if (BlockNThreat_RightBottom_TopLeft(column, row, chip, 0, 3, 3, 5, 4))
        {
            reward += POINTS_BLOCK_4_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatDiagonal RightBottom_TopLeft: " + 3);
        }
        else if (BlockNThreat_RightBottom_TopLeft(column, row, chip, 1, 2, 4, 4, 3))
        {
            reward += POINTS_BLOCK_3_IN_A_ROW;
            //Debug.LogWarning("Blocked IsThreatDiagonal RightBottom_TopLeft: " + 2);
        }

        return reward;
    }

    private int EvaluateCreatedThreats(int column, int row, int chip)
    {
        int reward = 0;

        // Gefahr nach links erstellt
        if (CreatesNThreatLeft(column, row, chip, 1, 4, 3))
        {
            reward += POINTS_THREAT_3_IN_A_ROW;
            //Debug.LogWarning("IsThreatToLeft: " + 3);
        }
        else if (CreatesNThreatLeft(column, row, chip, 2, 5, 2))
        {
            reward += POINTS_THREAT_2_IN_A_ROW;
            //Debug.LogWarning("IsThreatToLeft: " + 2);
        }

        // Gefahr nach rechts erstellt
        if (CreatesNThreatRight(column, row, chip, 2, 5, 3))
        {
            reward += POINTS_THREAT_3_IN_A_ROW;
            //Debug.LogWarning("IsThreatToRight: " + 3);
        }
        else if (CreatesNThreatRight(column, row, chip, 1, 4, 2))
        {
            reward += POINTS_THREAT_2_IN_A_ROW;
            //Debug.LogWarning("IsThreatToRight: " + 2);
        }

        // Gefahr vertikal erstellt
        if (CreatesNThreatVertical(column, row, chip, 2, 4, 3))
        {
            reward += POINTS_THREAT_3_IN_A_ROW_VERTICAL;
            //Debug.LogWarning("IsThreatVertical: " + 3);
        }
        else if (CreatesNThreatVertical(column, row, chip, 1, 3, 2))
        {
            reward += POINTS_THREAT_2_IN_A_ROW_VERTICAL;
            //Debug.LogWarning("IsThreatVertical: " + 2);
        }

        // Gefahr diagonal erstellt
        // Links unten nach Rechts oben
        if (CreatesNThreat_LeftBottom_TopRight(column, row, chip, 2, 2, 5, 4, 3))
        {
            reward += POINTS_THREAT_3_IN_A_ROW;
            //Debug.LogWarning("IsThreatDiagonal LeftBottom_TopRight: " + 3);
        }
        else if (CreatesNThreat_LeftBottom_TopRight(column, row, chip, 1, 1, 4, 3, 2))
        {
            reward += POINTS_THREAT_2_IN_A_ROW;
            //Debug.LogWarning("IsThreatDiagonal LeftBottom_TopRight: " + 2);
        }

        // Rechts unten nach Links oben
        if (CreatesNThreat_RightBottom_TopLeft(column, row, chip, 1, 2, 4, 4, 3))
        {
            reward += POINTS_THREAT_3_IN_A_ROW;
            //Debug.LogWarning("IsThreatDiagonal RightBottom_TopLeft: " + 3);
        }
        else if (CreatesNThreat_RightBottom_TopLeft(column, row, chip, 2, 1, 5, 3, 2))
        {
            reward += POINTS_THREAT_2_IN_A_ROW;
            //Debug.LogWarning("IsThreatDiagonal RightBottom_TopLeft: " + 2);
        }

        return reward;
    }

    bool CreatesNThreat_RightBottom_TopLeft(int column, int row, int chip, int minCol, int minRow, int maxCol, int maxRow, int n)
    {
        if (column >= minCol // Kann nur eine Gefahr sein, wenn nicht ganz links
            && column <= maxCol // Spalte ist nicht zu weit rechts
            && row >= minRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            && row <= maxRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            && board.State[column - 1, row + 1] == 0 // Chip daneben muss frei sein
            )
        {
            // Prüfe Chips rechts davon
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column + i, row - i] == chip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    bool BlockNThreat_RightBottom_TopLeft(int column, int row, int chip, int minCol, int minRow, int maxCol, int maxRow, int n)
    {
        if (column >= minCol // Kann nur eine Gefahr sein, wenn nicht ganz links
            && column <= maxCol // Spalte ist nicht zu weit rechts
            && row >= minRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            && row <= maxRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            )
        {
            // Prüfe Chips rechts oben davon
            int otherChip = board.redChip == chip ? board.yellowChip : board.redChip;
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column + i, row - i] == otherChip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    bool CreatesNThreat_LeftBottom_TopRight(int column, int row, int chip, int minCol, int minRow, int maxCol, int maxRow, int n)
    {
        if (column >= minCol // Kann nur eine Gefahr sein, wenn nicht ganz links
            && column <= maxCol // Spalte ist nicht zu weit rechts
            && row >= minRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            && row <= maxRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            && board.State[column + 1, row + 1] == 0 // Chip daneben muss frei sein
            )
        {
            // Prüfe Chips rechts davon
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column - i, row - i] == chip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    bool BlockNThreat_LeftBottom_TopRight(int column, int row, int chip, int minCol, int minRow, int maxCol, int maxRow, int n)
    {
        if (column >= minCol // Kann nur eine Gefahr sein, wenn nicht ganz links
            && column <= maxCol // Spalte ist nicht zu weit rechts
            && row >= minRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            && row <= maxRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            )
        {
            // Prüfe Chips rechts oben davon
            int otherChip = board.redChip == chip ? board.yellowChip : board.redChip;
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column - i, row - i] == otherChip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    bool BlockNThreatLeft(int column, int row, int chip, int minCol, int maxCol, int n)
    {
        if (column >= minCol // Kann nur eine Gefahr sein, wenn nicht ganz links
            && column <= maxCol // Spalte ist nicht zu weit rechts
            )
        {
            // Prüfe Chips rechts davon
            int otherChip = board.redChip == chip ? board.yellowChip : board.redChip;
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column + i, row] == otherChip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    bool CreatesNThreatLeft(int column, int row, int chip, int minCol, int maxCol, int n)
    {
        if (column >= minCol // Kann nur eine Gefahr sein, wenn nicht ganz links
            && column <= maxCol // Spalte ist nicht zu weit rechts
            && board.State[column - 1, row] == 0 // Chip daneben muss frei sein
            )
        {
            // Prüfe Chips rechts davon
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column + i, row] == chip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    bool CreatesNThreatRight(int column, int row, int chip, int minCol, int maxCol, int n)
    {
        if (column >= minCol // Kann nur eine Gefahr sein, wenn nicht ganz rechts
            && column <= maxCol // Spalte ist nicht zu weit rechts
            && board.State[column + 1, row] == 0 // Chip daneben muss frei sein
            )
        {
            // Prüfe Chips links davon
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column - i, row] == chip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    bool BlockNThreatRight(int column, int row, int chip, int minCol, int maxCol, int n)
    {
        if (column >= minCol // Kann nur eine Gefahr sein, wenn nicht ganz rechts
            && column <= maxCol // Spalte ist nicht zu weit rechts
            )
        {
            // Prüfe Chips links davon
            int otherChip = board.redChip == chip ? board.yellowChip : board.redChip;
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column - i, row] == otherChip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    bool CreatesNThreatVertical(int column, int row, int chip, int minRow, int maxRow, int n)
    {
        if (row >= minRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            && row <= maxRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            )
        {
            // Prüfe Chips darunter
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column, row - i] == chip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }
    
    bool BlockNThreatVertical(int column, int row, int chip, int minRow, int maxRow, int n)
    {
        if (row >= minRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            && row <= maxRow // Kann nur eine Gefahr sein, wenn mindestens in Reihe
            )
        {
            // Prüfe Chips darunter
            int otherChip = board.redChip == chip ? board.yellowChip : board.redChip;
            for (int i = 1; i < n; i++)
            {
                bool isSameChip = board.State[column, row - i] == otherChip;
                if (!isSameChip)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

}