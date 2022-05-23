using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class NetworkPlayer, that manages a player instance in the Game Server.
/// </summary>
public class NetworkPlayer : MonoBehaviour
{
    #region Variables
    [Header("Data")]
    [Tooltip("User data of the player")]
    private UserData user;

    [Tooltip("Object containing the constant boolInputs from the player")]
    private PlayerInputs inputs;


    [Header("Modifiable from Unity")]
    [Tooltip("Player's in game ID")]
    [SerializeField]
    private int ingameID;

    [Tooltip("List of Client Scripts to DELETE")]
    [SerializeField]
    private List<MonoBehaviour> clientScriptsToDelete = null;
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Start, that is executed before the first frame.
    /// </summary>
    private void Start()
    {
        // If not executing on the game server, delete this script.
        if (!EasyGameServerControl.instance.instanceType.Equals(EasyGameServerControl.EnumInstanceType.GameServer))
        {
            Destroy(this);
        }
        else
        {
            // Delete the scripts on the clientScriptsToDelete list.
            foreach (MonoBehaviour script in clientScriptsToDelete)
                Destroy(script);
        }
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the User.
    /// </summary>
    /// <returns>User</returns>
    public UserData GetUser() { return user; }

    /// <summary>
    /// Setter for the User.
    /// </summary>
    /// <param name="user">New User</param>
    public void SetUser(UserData user) { this.user = user; }

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
    /// Getter for the inputs.
    /// </summary>
    /// <returns>Inputs</returns>
    public PlayerInputs GetInputs() { return inputs; }

    /// <summary>
    /// Setter for the inputs.
    /// </summary>
    /// <param name="inputs">New inputs</param>
    public void SetInputs(PlayerInputs inputs) { this.inputs = inputs; }
    #endregion
}