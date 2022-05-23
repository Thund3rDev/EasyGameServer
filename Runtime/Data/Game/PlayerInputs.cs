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

    [Tooltip("Array of player boolInputs.")]
    [SerializeField]
    private bool[] boolInputs;
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
        boolInputs = inputs_;
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
    /// Getter for the bool Inputs.
    /// </summary>
    /// <returns>Player inputs</returns>
    public bool[] GetBoolInputs() { return boolInputs; }

    /// <summary>
    /// Setter for the bool Inputs.
    /// </summary>
    /// <param name="inputs">New player inputs</param>
    public void SetBoolInputs(bool[] inputs) { this.boolInputs = inputs; }
    #endregion
}
