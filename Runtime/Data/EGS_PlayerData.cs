using UnityEngine;

/// <summary>
/// Class EGS_PlayerData, that contains the information of a player to send.
/// </summary>
[System.Serializable]
public class EGS_PlayerData
{
    #region Variables
    [SerializeField]
    private int ingameID;

    [SerializeField]
    private string username;

    [SerializeField]
    private Vector3 position;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_PlayerData()
    {
        this.ingameID = -1;
        this.username = "";
        this.position = new Vector3();
    }

    /// <summary>
    /// Username Constructor
    /// </summary>
    public EGS_PlayerData(string username_)
    {
        this.ingameID = -1;
        this.username = username_;
        this.position = new Vector3();
    }

    /// <summary>
    /// Base Constructor
    /// </summary>
    public EGS_PlayerData(int ingameID_, string username_)
    {
        this.ingameID = ingameID_;
        this.username = username_;
        this.position = new Vector3();
    }

    /// <summary>
    /// Full Constructor
    /// </summary>
    public EGS_PlayerData(int ingameID_, string username_, Vector3 position_)
    {
        this.ingameID = ingameID_;
        this.username = username_;
        this.position = position_;
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
    /// Getter for the user name.
    /// </summary>
    /// <returns>User name</returns>
    public string GetUsername() { return username; }

    /// <summary>
    /// Setter for the user name.
    /// </summary>
    /// <param name="u">New User name</param>
    public void SetUsername(string u) { username = u; }

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
    #endregion

}
