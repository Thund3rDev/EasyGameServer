using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Class GameServerData, that stores the status of a GameServer and its control data.
/// </summary>
public class GameServerData
{
    #region Enums
    /// <summary>
    /// Enum EnumGameServerState, to define game server states.
    /// INACTIVE, LAUNCHED, CREATED, WAITING_PLAYERS, STARTED_GAME and FINISHED.
    /// </summary>
    public enum EnumGameServerState
    {
        INACTIVE,
        LAUNCHED,
        CREATED,
        WAITING_PLAYERS,
        STARTED_GAME,
        FINISHED
    }
    #endregion

    #region Variables
    [Header("Control")]
    [Tooltip("System process of the Game Server")]
    private Process process;

    [Tooltip("Status of the Game Server")]
    private EnumGameServerState status;

    [Tooltip("Game Server ID")]
    private int gameServerID;

    [Tooltip("Game Server IP Address")]
    private string ipAddress;


    [Header("Game")]
    [Tooltip("Room number")]
    private int room;

    [Tooltip("Game Server Data")]
    private GameFoundData gameFoundData;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public GameServerData()
    {
        this.gameServerID = -1;
        this.room = -1;
        this.status = EnumGameServerState.INACTIVE;
        this.ipAddress = "";
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    /// <param name="gameServerID">Game Server ID</param>
    /// <param name="gameFoundData"> Game Found Data</param>
    public GameServerData(int gameServerID, GameFoundData gameFoundData)
    {
        this.gameServerID = gameServerID;
        this.gameFoundData = gameFoundData;
        this.room = gameFoundData.GetRoom();
        this.status = EnumGameServerState.LAUNCHED;
        this.ipAddress = "";
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the process.
    /// </summary>
    /// <returns>GameServer process</returns>
    public Process GetProcess() { return process; }

    /// <summary>
    /// Setter for the process.
    /// </summary>
    /// <param name="process">New process</param>
    public void SetProcess(Process process) { this.process = process; }

    /// <summary>
    /// Getter for the status.
    /// </summary>
    /// <returns>GameServer status</returns>
    public EnumGameServerState GetStatus() { return status; }

    /// <summary>
    /// Setter for the status.
    /// </summary>
    /// <param name="status">New GameServer status</param>
    public void SetStatus(EnumGameServerState status) { this.status = status; }

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
    /// Getter for the GameServer IP Address.
    /// </summary>
    /// <returns>GameServer IP Address</returns>
    public string GetIPAddress() { return ipAddress; }

    /// <summary>
    /// Setter for the GameServer IP Address.
    /// </summary>
    /// <param name="ipAddress">New GameServer IP Address</param>
    public void SetIPAddress(string ipAddress) { this.ipAddress = ipAddress; }

    /// <summary>
    /// Getter for the room.
    /// </summary>
    /// <returns>Room number</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the room.
    /// </summary>
    /// <param name="room">New room</param>
    public void SetRoom(int room) { this.room = room; }

    /// <summary>
    /// Getter for the game found data.
    /// </summary>
    /// <returns>Game found data</returns>
    public GameFoundData GetGameFoundData() { return gameFoundData; }

    /// <summary>
    /// Setter for the game found data.
    /// </summary>
    /// <param name="gameFoundData">New game found data</param>
    public void SetGameFoundData(GameFoundData gameFoundData) { this.gameFoundData = gameFoundData; }
    #endregion
}
