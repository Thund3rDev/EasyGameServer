using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class EGS_Player, that manages a player instance in the Game Server.
/// </summary>
public class EGS_Player : MonoBehaviour
{
    #region Variables
    [Header("Data")]
    [Tooltip("User data of the player")]
    private EGS_User user;


    [Header("Modifiable from Unity")]
    [Tooltip("Player's in game ID")]
    [SerializeField] private int ingameID;

    [Tooltip("Client Scripts to DELETE")]
    [SerializeField] private List<MonoBehaviour> clientScriptsToDelete = null;

    [Tooltip("Array of player inputs")]
    private bool[] inputs; // TODO: Use EGS_PlayerInputs and permit different type of inputs (bool, float, string...).

    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Start, that is executed before the first frame.
    /// </summary>
    private void Start()
    {
        // If not executing on the game server, delete this script.
        if (!EGS_Control.instance.egs_type.Equals(EGS_Control.EGS_Type.GameServer))
        {
            Destroy(this);
        }
        else
        {
            foreach (MonoBehaviour script in clientScriptsToDelete)
                Destroy(script);
        }
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for User.
    /// </summary>
    /// <returns>User</returns>
    public EGS_User GetUser() { return user; }

    /// <summary>
    /// Setter for User.
    /// </summary>
    /// <param name="u">New User</param>
    public void SetUser(EGS_User u) { user = u; }

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
    /// Getter for the inputs.
    /// </summary>
    /// <returns>Inputs</returns>
    public bool[] GetInputs() { return inputs; }

    /// <summary>
    /// Setter for the inputs.
    /// </summary>
    /// <param name="i">New inputs</param>
    public void SetInputs(bool[] i) { inputs = i; }
    #endregion
}
