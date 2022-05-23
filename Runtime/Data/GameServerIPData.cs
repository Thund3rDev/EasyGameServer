using UnityEngine;

/// <summary>
/// Class GameServerIPData, that will store the Game Server Listener IP to exchange messages.
/// </summary>
public class GameServerIPData
{
    #region Variables
    [Header("Data")]
    [Tooltip("Game Server ID")]
    [SerializeField]
    private int gameServerID;

    [Tooltip("Game Server Listener IP")]
    [SerializeField]
    private string gameServerIP;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public GameServerIPData()
    {
        this.gameServerID = -1;
        this.gameServerIP = "";
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    public GameServerIPData(int gameServerID, string gameServerIP)
    {
        this.gameServerID = gameServerID;
        this.gameServerIP = gameServerIP;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the GameServer ID.
    /// </summary>
    /// <returns>GameServer ID</returns>
    public int GetGameServerID() { return gameServerID; }

    /// <summary>
    /// Setter for the GameServer ID.
    /// </summary>
    /// <param name="gameServerID">New GameServer ID</param>
    public void SetGameServerID(int gameServerID) { this.gameServerID = gameServerID; }

    /// <summary>
    /// Getter for the GameServer Listener IP.
    /// </summary>
    /// <returns>GameServer Listener IP</returns>
    public string GetGameServerIP() { return gameServerIP; }

    /// <summary>
    /// Setter for the GameServer Listener ID.
    /// </summary>
    /// <param name="gameServerID">New GameServer Listener IP</param>
    public void SetGameServerIP(string gameServerIP) { this.gameServerIP = gameServerIP; }
    #endregion

}
