using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class UpdateData, that contains the information of a game to be sent every server tick.
/// </summary>
[System.Serializable]
public class UpdateData
{
    #region Variables
    [Header("Update Data")]
    [Tooltip("List of Players data")]
    [SerializeField]
    private List<PlayerData> playersAtGame;

    [Tooltip("Game room")]
    [SerializeField]
    private int room;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public UpdateData()
    {
        this.playersAtGame = new List<PlayerData>();
        this.room = -1;
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    public UpdateData(int room)
    {
        this.playersAtGame = new List<PlayerData>();
        this.room = room;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the list of players at game.
    /// </summary>
    /// <returns>List of players at game</returns>
    public List<PlayerData> GetPlayersAtGame() { return playersAtGame; }

    /// <summary>
    /// Setter for the list of players at game.
    /// </summary>
    /// <param name="playersAtGame">New list of players at game</param>
    public void SetPlayersAtGame(List<PlayerData> playersAtGame) { this.playersAtGame = playersAtGame; }

    /// <summary>
    /// Getter for the game room.
    /// </summary>
    /// <returns>Game room</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the game room.
    /// </summary>
    /// <param name="room">New game room</param>
    public void SetRoom(int room) { this.room = room; }
    #endregion
}
