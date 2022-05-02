using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class EGS_User_Base, that contains the base structure of an user in the server.
/// </summary>
[System.Serializable]
public class EGS_User_Base
{
    #region Variables
    [Tooltip("User ID")]
    [SerializeField] protected int userID;

    [Tooltip("Socket connected to the server")]
    [SerializeField] protected Socket socket;

    [Tooltip("User IP Address")]
    [SerializeField] protected string ipAddress;

    [Tooltip("User name")]
    [SerializeField] protected string username;

    [Tooltip("Game room")]
    [SerializeField] protected int room;

    [Tooltip("Ingame ID")]
    [SerializeField] protected int ingameID;

    [Tooltip("Bool indicating if user left its game")]
    [SerializeField] protected bool leftGame;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_User_Base()
    {
        this.userID = -1;
        this.ipAddress = "";
        this.username = "";
        this.room = -1;
        this.ingameID = -1;
        this.leftGame = false;
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
    /// <param name="s">New user socket</param>
    public void SetSocket(Socket s) { socket = s; }

    /// <summary>
    /// Getter for the user IP Address.
    /// </summary>
    /// <returns>User IP Address</returns>
    public string GetIPAddress() { return ipAddress; }

    /// <summary>
    /// Setter for the user IP Address.
    /// </summary>
    /// <param name="u">New User IP Address</param>
    public void SetIPAddress(string i) { ipAddress = i; }

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

    /// <summary>
    /// Getter for the room.
    /// </summary>
    /// <returns>Room</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the room.
    /// </summary>
    /// <param name="r">New room</param>
    public void SetRoom(int r) { room = r; }

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

    /// <summary>
    /// Getter for the left game bool.
    /// </summary>
    /// <returns>Bool indicating if user left game</returns>
    public bool DidLeaveGame() { return leftGame; }

    /// <summary>
    /// Setter for the nleft game bool.
    /// </summary>
    /// <param name="l">New bool indicating if user left game</param>
    public void SetLeftGame(bool l) { leftGame = l; }
    #endregion

}
