using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class GameFoundData, that contains the information of a game when found to send.
/// </summary>
[System.Serializable]
public class GameFoundData
{
    #region Variables
    [Header("Control")]
    [Tooltip("List of users to Game")]
    [SerializeField]
    private List<UserData> usersToGame;

    [Tooltip("Room number")]
    [SerializeField]
    private int room;

    [Tooltip("Name of the scene where this game will be played")]
    [SerializeField]
    private string levelSceneName;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public GameFoundData()
    {
        this.usersToGame = new List<UserData>();
        this.room = -1;
        this.levelSceneName = "Level";
    }

    /// <summary>
    /// Base Constructor.
    /// </summary>
    public GameFoundData(int room, string levelSceneName)
    {
        this.usersToGame = new List<UserData>();
        this.room = room;
        this.levelSceneName = levelSceneName;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the list of users to game.
    /// </summary>
    /// <returns>List of users to game</returns>
    public List<UserData> GetUsersToGame() { return usersToGame; }

    /// <summary>
    /// Setter for the list of users to game.
    /// </summary>
    /// <param name="usersToGame">New list of users to game</param>
    public void SetUsersToGame(List<UserData> usersToGame) { this.usersToGame = usersToGame; }

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
    /// Getter for the level scene name.
    /// </summary>
    /// <returns>Level scene name</returns>
    public string GetLevelSceneName() { return levelSceneName; }

    /// <summary>
    /// Setter for the level scene name.
    /// </summary>
    /// <param name="levelSceneName">New level scene name</param>
    public void SetLevelSceneName(string levelSceneName) { this.levelSceneName = levelSceneName; }
    #endregion
}
