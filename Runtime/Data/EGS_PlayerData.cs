using UnityEngine;

/// <summary>
/// Class EGS_PlayerData, that contains the information of a player to send in game.
/// </summary>
[System.Serializable]
public class EGS_PlayerData
{
    #region Variables
    [Tooltip("Player's ingame ID")]
    [SerializeField] private int ingameID;

    [Tooltip("Player's position")]
    [SerializeField] private Vector3 position;

    [Tooltip("Player's direction")]
    [SerializeField] private Vector3 direction;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_PlayerData()
    {
        this.ingameID = -1;
        this.position = new Vector3();
        this.direction = new Vector3();
    }

    /// <summary>
    /// Base Constructor
    /// </summary>
    public EGS_PlayerData(int ingameID_)
    {
        this.ingameID = ingameID_;
        this.position = new Vector3();
        this.direction = new Vector3();
    }

    /// <summary>
    /// Full Constructor
    /// </summary>
    public EGS_PlayerData(int ingameID_, Vector3 position_, Vector3 direction_)
    {
        this.ingameID = ingameID_;
        this.position = position_;
        this.direction = direction_;
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
    /// Getter for the position.
    /// </summary>
    /// <returns>Position</returns>
    public Vector3 GetPosition() { return position; }

    /// <summary>
    /// Setter for the position.
    /// </summary>
    /// <param name="p">New position</param>
    public void SetPosition(Vector3 p) { position = p; }

    /// <summary>
    /// Getter for the direction.
    /// </summary>
    /// <returns>Direction</returns>
    public Vector3 GetDirection() { return direction; }

    /// <summary>
    /// Setter for the direction.
    /// </summary>
    /// <param name="d">New direction</param>
    public void SetDirection(Vector3 d) { direction = d; }
    #endregion
}
