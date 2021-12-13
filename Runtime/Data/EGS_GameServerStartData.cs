using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class EGS_GameServerStartData, that contains the information that the game server needs.
/// </summary>
[System.Serializable]
public class EGS_GameServerStartData
{
    #region Variables
    [SerializeField]
    private List<EGS_UserToGame> usersToGame;

    [SerializeField]
    private int room;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_GameServerStartData()
    {
        usersToGame = new List<EGS_UserToGame>();
        room = -1;
    }

    /// <summary>
    /// Base Constructor
    /// </summary>
    public EGS_GameServerStartData(int room_)
    {
        usersToGame = new List<EGS_UserToGame>();
        room = room_;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the list of users to game.
    /// </summary>
    /// <returns>List of users to game</returns>
    public List<EGS_UserToGame> GetUsersToGame() { return usersToGame; }

    /// <summary>
    /// Setter for the list of users to game.
    /// </summary>
    /// <param name="pag">New list of users to game</param>
    public void SetUsersToGame(List<EGS_UserToGame> pag) { usersToGame = pag; }

    /// <summary>
    /// Getter for the game room.
    /// </summary>
    /// <returns>Game room</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the game room.
    /// </summary>
    /// <param name="r">New game room</param>
    public void SetRoom(int r) { room = r; }
    #endregion

}
