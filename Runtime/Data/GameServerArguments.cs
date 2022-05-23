using UnityEngine;

/// <summary>
/// Class GameServerArguments, that will tell what port will use the Game Server Listener.
/// </summary>
public class GameServerArguments
{
    #region Variables
    [Header("Arguments")]
    [Tooltip("Game Server ID")]
    [SerializeField]
    private int gameServerID;

    [Tooltip("Game Server Listener Port")]
    [SerializeField]
    private int gameServerPort;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public GameServerArguments()
    {
        this.gameServerID = -1;
        this.gameServerPort = -1;
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    public GameServerArguments(int gameServerID, int gameServerPort)
    {
        this.gameServerID = gameServerID;
        this.gameServerPort = gameServerPort;
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
    /// Getter for the GameServer Listener Port.
    /// </summary>
    /// <returns>GameServer Listener IP</returns>
    public int GetGameServerPort() { return gameServerPort; }

    /// <summary>
    /// Setter for the GameServer Listener Port.
    /// </summary>
    /// <param name="gameServerPort">New GameServer Listener IP</param>
    public void SetGameServerPort(int gameServerPort) { this.gameServerPort = gameServerPort; }
    #endregion
}
