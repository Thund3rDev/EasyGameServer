using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class BaseUserData, that contains the base structure of an user in the server.
/// </summary>
[System.Serializable]
public class BaseUserData
{
    #region Variables
    [Header("Control")]
    [Tooltip("User ID")]
    [SerializeField]
    protected int userID;

    [Tooltip("Socket connected to the server")]
    [SerializeField]
    protected Socket socket;

    [Tooltip("User IP Address")]
    [SerializeField]
    protected string ipAddress;

    [Tooltip("User name")]
    [SerializeField]
    protected string username;

    [Tooltip("Game room")]
    [SerializeField]
    protected int room;

    [Tooltip("Ingame ID")]
    [SerializeField]
    protected int ingameID;

    [Tooltip("Bool indicating if user left its game")]
    [SerializeField]
    protected bool leftGame;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public BaseUserData()
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
    /// <param name="userID">New User ID</param>
    public void SetUserID(int userID) { this.userID = userID; }

    /// <summary>
    /// Getter for user socket.
    /// </summary>
    /// <returns>User socket</returns>
    public Socket GetSocket() { return socket; }

    /// <summary>
    /// Setter for user socket.
    /// </summary>
    /// <param name="socket">New user socket</param>
    public void SetSocket(Socket socket) { this.socket = socket; }

    /// <summary>
    /// Getter for the user IP Address.
    /// </summary>
    /// <returns>User IP Address</returns>
    public string GetIPAddress() { return ipAddress; }

    /// <summary>
    /// Setter for the user IP Address.
    /// </summary>
    /// <param name="ipAddress">New User IP Address</param>
    public void SetIPAddress(string ipAddress) { this.ipAddress = ipAddress; }

    /// <summary>
    /// Getter for the user name.
    /// </summary>
    /// <returns>User name</returns>
    public string GetUsername() { return username; }

    /// <summary>
    /// Setter for the user name.
    /// </summary>
    /// <param name="username">New User name</param>
    public void SetUsername(string username) { this.username = username; }

    /// <summary>
    /// Getter for the room.
    /// </summary>
    /// <returns>Room</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the room.
    /// </summary>
    /// <param name="room">New room</param>
    public void SetRoom(int room) { this.room = room; }

    /// <summary>
    /// Getter for the ingame ID.
    /// </summary>
    /// <returns>Ingame ID</returns>
    public int GetIngameID() { return ingameID; }

    /// <summary>
    /// Setter for the ingame ID.
    /// </summary>
    /// <param name="ingameID">New ingame ID</param>
    public void SetIngameID(int ingameID) { this.ingameID = ingameID; }

    /// <summary>
    /// Getter for the left game bool.
    /// </summary>
    /// <returns>Bool indicating if user left game</returns>
    public bool DidLeaveGame() { return leftGame; }

    /// <summary>
    /// Setter for the left game bool.
    /// </summary>
    /// <param name="leftGame">New bool indicating if user left game</param>
    public void SetLeftGame(bool leftGame) { this.leftGame = leftGame; }
    #endregion
}