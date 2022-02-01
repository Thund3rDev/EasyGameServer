using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class EGS_GameServerStartData, that contains the information about the players and the room number.
/// </summary>
[System.Serializable]
public class EGS_GameServerStartData
{
    #region Variables
    [Tooltip("Player's in game ID")]
    [SerializeField] private List<EGS_PlayerToGame> playersToGame;

    [Tooltip("Game room")]
    [SerializeField] private int room;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_GameServerStartData()
    {
        playersToGame = new List<EGS_PlayerToGame>();
        room = -1;
    }

    /// <summary>
    /// Base Constructor
    /// </summary>
    public EGS_GameServerStartData(int room_)
    {
        playersToGame = new List<EGS_PlayerToGame>();
        room = room_;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the list of players to game.
    /// </summary>
    /// <returns>List of players to game</returns>
    public List<EGS_PlayerToGame> GetPlayersToGame() { return playersToGame; }

    /// <summary>
    /// Setter for the list of players to game.
    /// </summary>
    /// <param name="pag">New list of players to game</param>
    public void SetPlayersToGame(List<EGS_PlayerToGame> pag) { playersToGame = pag; }

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
