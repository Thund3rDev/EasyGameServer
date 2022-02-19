using System.IO;
using System.Net.Sockets;
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
    public static EGS_Client client_instance;

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

    // User Data.
    // TODO: Put this in an object and save an EGS_USER instance and other variables.
    public string username;
    public int ingameID;
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, executed on script load.
    /// </summary>
    private void Awake()
    {
        if (client_instance == null)
        {
            client_instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
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
        // Test.
        // Get player username.
        node = doc.DocumentElement.SelectSingleNode("//client/username");
        username = node.InnerText;
    }
    #endregion
}
