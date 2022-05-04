using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class BaseAgent : Agent
{
    /// <summary>
    /// Environment
    /// </summary>
    public Board Board { get; set; }
    /// <summary>
    /// Welcher Spieler bin ich?
    /// </summary>
    public Player Player { get; set; }
    /// <summary>
    /// Wie heiße ich?
    /// </summary>
    public string PlayerName { get; set; }
    public bool IsThinking { get; internal set; }

    /// <summary>
    /// weird
    /// </summary>
    //public static bool IsThinking { get; set; }

    /// <summary>
    /// Initialisierung
    /// </summary>
    public override void OnEpisodeBegin()
    {
        Player = Board.CurrentPlayer;
    }

    /// <summary>
    /// Führt eine Aktion aus.
    /// Wirft einen Chip in eine Spalte.
    /// Das verhalten kommt aus dem trainierten Modell
    /// oder der Heuristic()
    /// </summary>
    /// <param name="actions"></param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        Board.SelectColumn(actions.DiscreteActions[0], Player);
        IsThinking = false;
    }

    /// <summary>
    /// Welche Spalten können nicht verwendet werden,
    /// weil sie bereits die Maximalanzahl an Chips beinhalten.
    /// => Deaktiviere diese.
    /// 
    /// Man könnte das theoretisch auch erlernen, indem man prüft,
    /// ob der Chip sich außerhalb der Observations befindet und
    /// dann extrem hoch negativ bestraft.
    /// Das mache ich hier aber nicht.
    /// </summary>
    /// <param name="actionMask"></param>
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        foreach (int columnIndex in Board.GetOccupiedColumns())
        {
            // Spalte wird zur Auswahl deaktiviert
            actionMask.SetActionEnabled(0, columnIndex, false);
        }
    }
}
