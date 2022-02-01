using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Class EGS_GameServerData, that stores the status of a GameServer and its control data.
/// </summary>
public class EGS_GameServerData
{
    /// <summary>
    /// Enum EGS_GameServerState, to define game server states.
    /// </summary>
    public enum EGS_GameServerState
    {
        INACTIVE,
        LAUNCH,
        CREATED,
        WAITING_PLAYERS,
        STARTED,
        FINISHED
    }

    #region Variables
    [Tooltip("System process of the Game Server")]
    private Process process;

    [Tooltip("Status of the Game Server")]
    private EGS_GameServerState status;

    [Tooltip("Game Server ID")]
    private int gameServerID;

    [Tooltip("Room number")]
    private int room;
    #endregion

    #region Constructors

    /// <summary>
    /// Base Constructor.
    /// </summary>
    /// <param name="gameServerID_">Game Server ID</param>
    /// <param name="room_">Room number</param>
    public EGS_GameServerData(int gameServerID_, int room_)
    {
        gameServerID = gameServerID_;
        room = room_;
        status = EGS_GameServerState.LAUNCH;
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
    /// <param name="p">New process</param>
    public void SetProcess(Process p) { process = p; }

    /// <summary>
    /// Getter for the status.
    /// </summary>
    /// <returns>GameServer status</returns>
    public EGS_GameServerState GetStatus() { return status; }

    /// <summary>
    /// Setter for the status.
    /// </summary>
    /// <param name="s">New GameServer status</param>
    public void SetStatus(EGS_GameServerState s) { status = s; }

    /// <summary>
    /// Getter for the GameServer ID.
    /// </summary>
    /// <returns>GameServer ID</returns>
    public int GetGameServerID() { return gameServerID; }

    /// <summary>
    /// Setter for the GameServer ID.
    /// </summary>
    /// <param name="g">New GameServer ID</param>
    public void SetGameServerID(int g) { gameServerID = g; }

    /// <summary>
    /// Getter for the room.
    /// </summary>
    /// <returns>Room number</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the room.
    /// </summary>
    /// <param name="g">New room</param>
    public void SetRoom(int r) { room = r; }
    #endregion
}
