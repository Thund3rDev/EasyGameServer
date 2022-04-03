using UnityEngine;

// Modifiable
public class EGS_PlayerInputs
{
    #region Variables
    [Tooltip("Player's ingame ID")]
    [SerializeField] private int ingameID;

    [Tooltip("Array of player inputs.")]
    [SerializeField] private bool[] inputs;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public EGS_PlayerInputs()
    {
        ingameID = -1;
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    public EGS_PlayerInputs(int ingameID_, bool[] inputs_)
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
    /// <param name="i">New ingame ID</param>
    public void SetIngameID(int i) { ingameID = i; }

    /// <summary>
    /// Getter for the Inputs.
    /// </summary>
    /// <returns>Inputs</returns>
    public bool[] GetInputs() { return inputs; }

    /// <summary>
    /// Setter for the Inputs.
    /// </summary>
    /// <param name="m">New Inputs</param>
    public void SetInputs(bool[] m) { inputs = m; }
    #endregion
}
