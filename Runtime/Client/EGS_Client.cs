using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

/// <summary>
/// Class EGS_Client, that controls the client side.
/// </summary>
public class EGS_Client : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_Client instance;

    [Tooltip("Struct that contains the server data")]
    public static EGS_Config serverData;


    [Header("Networking")]
    [Tooltip("Bool that indicates if client is connnected to the master server")]
    public bool connectedToMasterServer = false;
    [Tooltip("Bool that indicates if client is connnected to the game server")]
    public bool connectedToGameServer = false;
    [Tooltip("Bool that indicates if client is connnected to a server")]
    public bool connectedToServer = false;

    [Tooltip("Controller for client socket")]
    public EGS_CL_Sockets clientSocketController = null;


    [Header("Client Data")]
    [Tooltip("Instance of the EGS User for this client")]
    private EGS_User user;

    [Tooltip("Long that stores the time a RTT lasts since server ask and receive")]
    private long clientPing;


    [Header("Game Data")]
    [Tooltip("Dictionary of player usernames by their ingameID")]
    private Dictionary<int, string> playerUsernames = new Dictionary<int, string>();
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, executed on script load.
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        user = new EGS_User();
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method ConnectToServer, that prepares all client data and try to connect to the server.
    /// </summary>
    public void ConnectToServer()
    {
        // Check if server already started.
        if (connectedToMasterServer)
        {
            Debug.LogWarning("Client already connected.");
            return;
        }

        // Establish the connection to the server.
        connectedToMasterServer = true;

        // Read server config data.
        ReadServerData();

        // Create client socket manager
        clientSocketController = new EGS_CL_Sockets();
        // Connect to the server
        clientSocketController.ConnectToServer();
    }

    /// <summary>
    /// Method DisconnectFromServer, that will stop sending messages and listening.
    /// </summary>
    public void DisconnectFromServer()
    {
        // If not connected to server, return.
        if (!connectedToMasterServer)
            return;

        // Disconnect from server.
        connectedToMasterServer = false;
        clientSocketController.DisconnectFromServer();
    }

    /// <summary>
    /// Method SendMessage, that will send a message to the server
    /// </summary>
    /// <param name="type">String that contains the type of the message</param>
    /// <param name="msg">String that contains the message itself</param>
    public void SendMessage(string type, string msg)
    {
        // Send the message by the socket controller.
        clientSocketController.SendMessage(type, msg);
    }

    /// <summary>
    /// Method SendMessage, that will send a message to the server
    /// </summary>
    /// <param name="messageToSend">Message to send to the server</param>
    public void SendMessage(EGS_Message messageToSend)
    {
        // Send the message by the socket controller.
        clientSocketController.SendMessage(messageToSend);
    }

    /// <summary>
    /// Method JoinQueue, that will ask the server for a game.
    /// </summary>
    public void JoinQueue()
    {
        SendMessage("QUEUE_JOIN", "");
    }

    /// <summary>
    /// Method LeaveQueue, to stop searching a game.
    /// </summary>
    public void LeaveQueue()
    {
        SendMessage("QUEUE_LEAVE", "");
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method ReadServerData, to load all server config data.
    /// </summary>
    private void ReadServerData()
    {
        // Read server config data.
        string configXMLPath = "Packages/com.thund3r.easy_game_server/config.xml";
        if (!File.Exists("Packages/com.thund3r.easy_game_server/config.xml"))
            configXMLPath = "./config.xml";

        XmlDocument doc = new XmlDocument();
        doc.Load(configXMLPath);

        XmlNode node;

        /// Server Data.
        // Get time between round trip times.
        node = doc.DocumentElement.SelectSingleNode("//server/time-between-rtt");
        EGS_Config.TIME_BETWEEN_RTTS = int.Parse(node.InnerText);

        /// Networking Data.
        // Get server ip.
        node = doc.DocumentElement.SelectSingleNode("//networking/server-ip");
        EGS_Config.serverIP = node.InnerText;

        // Get server port.
        node = doc.DocumentElement.SelectSingleNode("//networking/base-port");
        EGS_Config.serverPort = int.Parse(node.InnerText);

        /// Client Data.
        // TODO: Make possible different ways to get the username.
        // TEST.
        // Get player username.
        node = doc.DocumentElement.SelectSingleNode("//client/username");
        user.SetUsername(node.InnerText);
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the client user.
    /// </summary>
    /// <returns>Client user</returns>
    public EGS_User GetUser()
    {
        return user;
    }

    /// <summary>
    /// Setter for the client user.
    /// </summary>
    /// <param name="u">New client user</param>
    public void SetUser(EGS_User u)
    {
        user = u;
    }

    /// <summary>
    /// Getter for the client ping.
    /// </summary>
    /// <returns>Client ping</returns>
    public long GetClientPing()
    {
        return clientPing;
    }

    /// <summary>
    /// Setter for the client ping.
    /// </summary>
    /// <param name="p">New client ping</param>
    public void SetClientPing(long p)
    {
        clientPing = p;
    }

    /// <summary>
    /// Getter for the player usernames dictionary.
    /// </summary>
    /// <returns>Dictionary of player usernames</returns>
    public Dictionary<int, string> GetPlayerUsernames()
    {
        return playerUsernames;
    }

    /// <summary>
    /// Setter for the player usernames dictionary.
    /// </summary>
    /// <param name="p">New dictionary of player usernames</param>
    public void SetPlayerUsernames(Dictionary<int, string> p)
    {
        playerUsernames = p;
    }
    #endregion
}
