using UnityEngine;
/// <summary>
/// Class EGS_UserToGame, that contains the structure of a player to be sent to the Game Server.
/// </summary>
[System.Serializable]
public class EGS_UserToGame
{
    #region Variables
    // User.
    [SerializeField]
    private EGS_User user;
    // Ingame ID.
    [SerializeField]
    private int ingameID;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_UserToGame(EGS_User user_, int ingameID_)
    {
        this.user = user_;
        this.ingameID = ingameID_;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for User.
    /// </summary>
    /// <returns>Use</returns>
    public EGS_User GetUser() { return user; }

    /// <summary>
    /// Setter for User.
    /// </summary>
    /// <param name="u">New User</param>
    public void SetUser(EGS_User u) { user = u; }

    /// <summary>
    /// Getter for Ingame ID.
    /// </summary>
    /// <returns>Ingame ID</returns>
    public int GetIngameID() { return ingameID; }

    /// <summary>
    /// Setter for Ingame ID.
    /// </summary>
    /// <param name="i">New Ingame ID</param>
    public void SetIngameID(int i) { ingameID = i; }
    #endregion

}
