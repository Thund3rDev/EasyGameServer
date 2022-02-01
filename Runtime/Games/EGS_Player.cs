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
    public int ingameID;
    [Tooltip("Client Scripts to DELETE")]
    public List<MonoBehaviour> clientScriptsToDelete = null;

    [Header("Physics, movement and control")]
    [Tooltip("Player Speed")]
    private float speed = 3f; // TODO: Distinguish between defaultSpeed and currentSpeed.
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

    #region Class Methods
    /// <summary>
    /// Method CalculatePosition, that receives the TICK_RATE and calculates the player's position.
    /// </summary>
    /// <param name="TICK_RATE">Tick rate: Miliseconds between executions.</param>
    public void CalculatePosition(float TICK_RATE)
    {
        // Calculate movement by inputs.
        Vector3 movement = new Vector3();

        if (inputs[0])
            movement.y += 1;

        if (inputs[1])
            movement.y -= 1;

        if (inputs[2])
            movement.x -= 1;

        if (inputs[3])
            movement.x += 1;

        // Multiply movement by speed having in count the tick rate.
        movement *= (speed * (TICK_RATE / 1000));

        // Calculate new position and move the player.
        this.transform.position += movement;
    }

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
    #endregion
}
