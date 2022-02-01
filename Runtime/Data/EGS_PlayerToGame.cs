using UnityEngine;

/// <summary>
/// Class EGS_PlayerToGame, that contains the information of a player for a game.
/// </summary>
[System.Serializable]
public class EGS_PlayerToGame
{
    #region Variables
    [Tooltip("User data of the player")]
    [SerializeField] private EGS_User user;

    [Tooltip("Player's in game ID")]
    [SerializeField] private int ingameID;
    #endregion

    #region Constructors
    /// <summary>
    /// Base Constructor.
    /// </summary>
    public EGS_PlayerToGame(EGS_User user_)
    {
        this.user = user_;
        this.ingameID = -1;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for User.
    /// </summary>
    /// <returns>User</returns>
    public EGS_User GetUser() { return user; }

    /// <summary>
    /// Setter for User.
    /// </summary>
    /// <param name="u">New User</param>
    public void SetUser(EGS_User u) { user = u; }

    /// <summary>
    /// Getter for the ingame ID.
    /// </summary>
    /// <returns>Ingame ID</returns>
    public int GetIngameID() { return ingameID; }

    /// <summary>
    /// Setter for the ingame ID.
    /// </summary>
    /// <param name="i">New ingame ID</param>
    public void SetIngameID(int i) { ingameID = i; }
    #endregion
}
