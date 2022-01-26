using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class ScoreAgent : BaseAgent
{
    /// <summary>
    /// Environment
    /// </summary>
    public Board Model { get; set; }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        Model = new Board();
    }

    /// <summary>
    /// Verwendet eine zufällige Spalte.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Speichern der Umgebung im Modell
        SaveEnvironmentModel();

        // Berechne für die einzelnen Spalten den Reward
        List<int> availableColumns = Model.GetAvailableColumns().ToList();
        List<int> rewards = new List<int>();
        foreach (int column in availableColumns)
        {
            int row = Model.InsertChip(column);
            int reward = Model.Evaluator.EvaluateReward(column, row);

            rewards.Add(reward);

            // Zurücksetzen des Modells
            SaveEnvironmentModel();
        }

        // Spalte wählen
        int maximumReward = rewards.Max();
        List<int> columnsWithMaxReward = new List<int>();
        // Welche Spalten haben den gefundenen maximalen reward
        for (int i = 0; i < rewards.Count; i++)
        {
            if (maximumReward == rewards[i])
            {
                columnsWithMaxReward.Add(availableColumns[i]);
            }
        }

        // Für eine der Spalten entscheiden
        // Wenn gleicher Reward, dann Zufall.
        int index = Random.Range(0, columnsWithMaxReward.Count);
        int pickedColumn = columnsWithMaxReward[index];

        // Spalte setzen
        actionsOut.DiscreteActions.Array[0] = pickedColumn;
    }

    public void SaveEnvironmentModel()
    {
        Model.State = Board.State.Clone() as int[,];
        Model.CurrentPlayer = Board.CurrentPlayer;
        Model.emptyChip = Board.emptyChip;
        Model.redChip = Board.redChip;
        Model.yellowChip = Board.yellowChip;
    }

    /// <summary>
    /// Wird beim RL für die Entscheidung verwendet.
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // 6 * 7 Werte = 42 ints
        foreach (int fieldStatus in Board.GetBoardStateAs1DArray(Player))
        {
            // Status des Feldes
            // 3: Anzahl möglicher Feldwerte
            // - 0: Kein Chip
            // - 1: Eigener Chip
            // - 2: Fremder Chip
            sensor.AddOneHotObservation(fieldStatus, 3);
        }
    }
}
