using UnityEngine;

/// <summary>
/// Class PlayerData, that contains the information of a player to be sent in game.
/// </summary>
[System.Serializable]
public class PlayerData
{
    #region Variables
    [Header("Player")]
    [Tooltip("Player's ingame ID")]
    [SerializeField]
    private int ingameID;

    [Tooltip("Player's position")]
    [SerializeField]
    private Vector3 position;

    [Tooltip("Player's direction")]
    [SerializeField]
    private Vector3 direction;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public PlayerData()
    {
        this.ingameID = -1;
        this.position = new Vector3();
        this.direction = new Vector3();
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    public PlayerData(int ingameID_)
    {
        this.ingameID = ingameID_;
        this.position = new Vector3();
        this.direction = new Vector3();
    }

    /// <summary>
    /// Full Constructor.
    /// </summary>
    public PlayerData(int ingameID_, Vector3 position_, Vector3 direction_)
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
    /// <param name="ingameID">New ingame ID</param>
    public void SetIngameID(int ingameID) { this.ingameID = ingameID; }

    /// <summary>
    /// Getter for the position.
    /// </summary>
    /// <returns>Current position</returns>
    public Vector3 GetPosition() { return position; }

    /// <summary>
    /// Setter for the position.
    /// </summary>
    /// <param name="position">New position</param>
    public void SetPosition(Vector3 position) { this.position = position; }

    /// <summary>
    /// Getter for the direction.
    /// </summary>
    /// <returns>Current direction</returns>
    public Vector3 GetDirection() { return direction; }

    /// <summary>
    /// Setter for the direction.
    /// </summary>
    /// <param name="direction">New direction</param>
    public void SetDirection(Vector3 direction) { this.direction = direction; }
    #endregion
}
