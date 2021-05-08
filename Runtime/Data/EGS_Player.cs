using System.Net.Sockets;
using UnityEngine;
/// <summary>
/// Class EGS_Player, that contains the structure of a player in a game.
/// </summary>
public class EGS_Player
{
    #region Variables
    // User assigned to the player.
    private EGS_User user;

    // Position of the player ingame
    private Vector3 position;

    // Speed of the player
    private float speed;

    // Inputs
    private bool[] inputs;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_Player(EGS_User user_)
    {
        this.user = user_;
        this.position = new Vector3();
        this.speed = 0.002f;
        this.inputs = new bool[4];
    }
    #endregion

    #region Class Methods
    public void CalculatePosition(float TICK_RATE)
    {
        // Calculate movement.
        Vector3 movement = new Vector3();

        if (inputs[0])
            movement.y += 1;

        if (inputs[1])
            movement.y -= 1;

        if (inputs[2])
            movement.x -= 1;

        if (inputs[3])
            movement.x += 1;

        // Multiply by speed.
        movement *= (speed * TICK_RATE);

        // Calculate new position
        position += movement;
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
    /// Getter for Position.
    /// </summary>
    /// <returns>Position</returns>
    public Vector3 GetPosition() { return position; }

    /// <summary>
    /// Setter for Position.
    /// </summary>
    /// <param name="p">New Position</param>
    public void SetPosition(Vector3 p) { position = p; }

    /// <summary>
    /// Getter for Speed.
    /// </summary>
    /// <returns>Speed</returns>
    public float GetSpeed() { return speed; }

    /// <summary>
    /// Setter for Speed.
    /// </summary>
    /// <param name="s">New Speed</param>
    public void SetSpeed(float s) { speed = s; }

    /// <summary>
    /// Getter for inputs.
    /// </summary>
    /// <returns>Inputs</returns>
    public bool[] GetInputs() { return inputs; }

    /// <summary>
    /// Setter for inputs.
    /// </summary>
    /// <param name="b">New Inputs</param>
    public void SetInputs(bool[] b) { inputs = b; }
    #endregion

}
