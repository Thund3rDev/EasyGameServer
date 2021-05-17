using System.Net.Sockets;
using UnityEngine;
/// <summary>
/// Class EGS_User, that contains the structure of a player in the server.
/// </summary>
[System.Serializable]
public class EGS_User
{
    #region Variables
    // User ID.
    [SerializeField]
    private int userID;
    // Socket connected to the server.
    [SerializeField]
    private Socket socket;
    // User name.
    [SerializeField]
    private string username;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_User()
    {
        this.userID = 0;
        this.username = "";
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for User ID.
    /// </summary>
    /// <returns>User ID</returns>
    public int GetUserID() { return userID; }

    /// <summary>
    /// Setter for User ID.
    /// </summary>
    /// <param name="u">New User ID</param>
    public void SetUserID(int u) { userID = u; }

    /// <summary>
    /// Getter for user socket.
    /// </summary>
    /// <returns>User socket</returns>
    public Socket GetSocket() { return socket; }

    /// <summary>
    /// Setter for user socket.
    /// </summary>
    /// <param name="u">New user socket</param>
    public void SetSocket(Socket s) { socket = s; }

    /// <summary>
    /// Getter for the user name.
    /// </summary>
    /// <returns>User name</returns>
    public string GetUsername() { return username; }

    /// <summary>
    /// Setter for the user name.
    /// </summary>
    /// <param name="u">New User name</param>
    public void SetUsername(string u) { username = u; }
    #endregion

}
