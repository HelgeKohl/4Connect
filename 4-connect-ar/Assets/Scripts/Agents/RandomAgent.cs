using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class RandomAgent : BaseAgent
{
    /// <summary>
    /// Verwendet eine zufällige Spalte.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int[] availableColumns = Board.GetAvailableColumns().ToArray();

        int index = Random.Range(0, availableColumns.Length);
        int pickedColumn = availableColumns[index];

        actionsOut.DiscreteActions.Array[0] = pickedColumn;
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