using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class EGS_UpdateData, that contains the information of a game to send.
/// </summary>
[System.Serializable]
public class EGS_UpdateData
{
    #region Variables
    [SerializeField]
    private List<EGS_PlayerData> playersAtGame;

    [SerializeField]
    private int room;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_UpdateData()
    {
        playersAtGame = new List<EGS_PlayerData>();
        room = -1;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the list of players at game.
    /// </summary>
    /// <returns>List of players at game</returns>
    public List<EGS_PlayerData> GetPlayersAtGame() { return playersAtGame; }

    /// <summary>
    /// Setter for the list of players at game.
    /// </summary>
    /// <param name="pag">New list of players at game</param>
    public void SetPlayersAtGame(List<EGS_PlayerData> pag) { playersAtGame = pag; }

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
