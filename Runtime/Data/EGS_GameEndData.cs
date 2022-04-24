using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EGS_GameEndData
{
    #region Variables
    [Tooltip("List of game order as a list of their player IDs")]
    [SerializeField]
    private List<int> playerIDsOrderList;

    [Tooltip("Game Server ID")]
    [SerializeField]
    private int gameServerID;

    [Tooltip("Room number")]
    [SerializeField] private int room;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_GameEndData()
    {
        this.gameServerID = -1;
        this.room = -1;
        this.playerIDsOrderList = new List<int>();
    }

    /// <summary>
    /// Base Constructor
    /// </summary>
    public EGS_GameEndData(int gameServerID_)
    {
        this.gameServerID = gameServerID_;
        this.room = -1;
        this.playerIDsOrderList = new List<int>();
    }

    /// <summary>
    /// Full Constructor
    /// </summary>
    public EGS_GameEndData(int gameServerID_, int room_, List<int> userIDsOrderList_)
    {
        this.gameServerID = gameServerID_;
        this.room = room_;
        this.playerIDsOrderList = new List<int>(userIDsOrderList_);
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the Player IDs ordered list.
    /// </summary>
    /// <returns>Player IDs ordered list</returns>
    public List<int> GetPlayerIDsOrderList() { return playerIDsOrderList; }

    /// <summary>
    /// Setter for the UserIDs ordered list.
    /// </summary>
    /// <param name="p">New Player IDs ordered list</param>
    public void SetPlayerIDsOrderList(List<int> p) { playerIDsOrderList = p; }

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
    /// Getter for the room number.
    /// </summary>
    /// <returns>Room number</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the room number.
    /// </summary>
    /// <param name="room_">New room number</param>
    public void SetRoom(int room_) { room = room_; }
    #endregion
}
