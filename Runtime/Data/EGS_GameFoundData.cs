using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class EGS_GameFoundData, that contains the information of a game when found to send.
/// </summary>
[System.Serializable]
public class EGS_GameFoundData
{
    #region Variables
    [Tooltip("List of users to Game")]
    [SerializeField] private List<EGS_User> usersToGame;

    [Tooltip("Room number")]
    [SerializeField] private int room;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_GameFoundData()
    {
        usersToGame = new List<EGS_User>();
    }

    /// <summary>
    /// Base Constructor
    /// </summary>
    public EGS_GameFoundData(int room_)
    {
        usersToGame = new List<EGS_User>();
        room = room_;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the list of users to game.
    /// </summary>
    /// <returns>List of users to game</returns>
    public List<EGS_User> GetUsersToGame() { return usersToGame; }

    /// <summary>
    /// Setter for the list of users to game.
    /// </summary>
    /// <param name="pag">New list of users to game</param>
    public void SetUsersToGame(List<EGS_User> utg) { usersToGame = utg; }

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
