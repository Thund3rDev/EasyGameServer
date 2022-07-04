using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class GameEndData, that contains properties that will be shared
/// to the players and to the master server when the game ends.
/// </summary>
[System.Serializable]
public class GameEndData
{
    #region Variables
    [Header("Control")]
    [Tooltip("List of game order as a list of their player IDs (0 is winner, 1 is second...)")]
    [SerializeField]
    private List<int> playerIDsOrderList;

    [Tooltip("Game Server ID")]
    [SerializeField]
    private int gameServerID;

    [Tooltip("Room number")]
    [SerializeField]
    private int room;

    [Tooltip("Bool that indicates if the game endedAsDisconnection")]
    [SerializeField]
    private bool endedAsDisconnection;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public GameEndData()
    {
        this.gameServerID = -1;
        this.room = -1;
        this.playerIDsOrderList = new List<int>();
        this.endedAsDisconnection = false;
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    public GameEndData(int gameServerID)
    {
        this.gameServerID = gameServerID;
        this.room = -1;
        this.playerIDsOrderList = new List<int>();
        this.endedAsDisconnection = false;
    }

    /// <summary>
    /// Full Constructor.
    /// </summary>
    public GameEndData(int gameServerID, int room, List<int> userIDsOrderList, bool endedAsDisconnection)
    {
        this.gameServerID = gameServerID;
        this.room = room;
        this.playerIDsOrderList = new List<int>(userIDsOrderList);
        this.endedAsDisconnection = endedAsDisconnection;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the Player IDs ordered list.
    /// </summary>
    /// <returns>Player IDs ordered list</returns>
    public List<int> GetPlayerIDsOrderList() { return playerIDsOrderList; }

    /// <summary>
    /// Setter for the PlayerIDs ordered list.
    /// </summary>
    /// <param name="playerIDsOrderList">New Player IDs ordered list</param>
    public void SetPlayerIDsOrderList(List<int> playerIDsOrderList) { this.playerIDsOrderList = playerIDsOrderList; }

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
    /// Getter for the room number.
    /// </summary>
    /// <returns>Room number</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the room number.
    /// </summary>
    /// <param name="room">New room number</param>
    public void SetRoom(int room) { this.room = room; }

    /// <summary>
    /// Getter for the endedAsDisconnection bool.
    /// </summary>
    /// <returns>Bool that indicates if the game ended as disconnection</returns>
    public bool GetEndedAsDisconnection() { return endedAsDisconnection; }

    /// <summary>
    /// Setter for the endedAsDisconnection bool.
    /// </summary>
    /// <param name="endedAsDisconnection">New endedAsDisconnection value</param>
    public void SetEndedAsDisconnection(bool endedAsDisconnection) { this.endedAsDisconnection = endedAsDisconnection; }
    #endregion
}
