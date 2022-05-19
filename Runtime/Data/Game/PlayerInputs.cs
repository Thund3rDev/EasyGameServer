using UnityEngine;

/// <summary>
/// Class PlayerInputs, that defines how inputs are stored.
/// [MODIFIABLE]
/// </summary>
public class PlayerInputs
{
    #region Variables
    [Header("Player Inputs")]
    [Tooltip("Player's ingame ID")]
    [SerializeField]
    private int ingameID;

    [Tooltip("Array of player inputs.")]
    [SerializeField]
    private bool[] inputs;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public PlayerInputs()
    {
        ingameID = -1;
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    public PlayerInputs(int ingameID_, bool[] inputs_)
    {
        ingameID = ingameID_;
        inputs = inputs_;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the ingame ID.
    /// </summary>
    /// <returns>Ingame ID</returns>
    public int GetIngameID() { return ingameID; }

    /// <summary>
    /// Setter for the ingame ID.
    /// </summary>
    /// <param name="ingameID">New ingame ID</param>
    public void SetIngameID(int ingameID) { this.ingameID = ingameID; }

    /// <summary>
    /// Getter for the Inputs.
    /// </summary>
    /// <returns>Player inputs</returns>
    public bool[] GetInputs() { return inputs; }

    /// <summary>
    /// Setter for the Inputs.
    /// </summary>
    /// <param name="inputs">New player inputs</param>
    public void SetInputs(bool[] inputs) { this.inputs = inputs; }
    #endregion
}
