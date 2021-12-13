using System.Net.Sockets;
using UnityEngine;
/// <summary>
/// Class EGS_Player, that contains the structure of a player in a game.
/// </summary>
public class EGS_Player
{
    #region Variables
    private EGS_User user;

    private int room; // -1 means no room.
    private int ingameID; // -1 means not ingame.
    private Vector3 position;
    private float speed;
    private bool[] inputs;
    #endregion

    #region Constructors
    /// <summary>
    /// Base User Constructor
    /// </summary>
    public EGS_Player(EGS_User user_)
    {
        this.user = user_;

        this.room = -1;
        this.ingameID = -1;
        this.position = new Vector3();
        this.speed = 0.002f;
        this.inputs = new bool[4];
    }

    /// <summary>
    /// User and ingameID Constructor
    /// </summary>
    public EGS_Player(EGS_User user_, int ingameID_)
    {
        this.user = user_;

        this.room = -1;
        this.ingameID = ingameID_;
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
    /// Getter for Room.
    /// </summary>
    /// <returns>Room</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for Room.
    /// </summary>
    /// <param name="r">New Room</param>
    public void SetRoom(int r) { room = r; }

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
