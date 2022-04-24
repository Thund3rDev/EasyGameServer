using System;
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
  

    [Header("Networking")]
    [Tooltip("Bool that indicates if client is connectingToServer")]
    public bool connectingToServer = false;
    [Tooltip("Bool that indicates if client is connected to the master server")]
    public bool connectedToMasterServer = false;
    [Tooltip("Bool that indicates if client is connected to the game server")]
    public bool connectedToGameServer = false;

    [Tooltip("Integer that indicates the number of current tries to connect to the server")]
    private int currentConnectionTries;

    [Tooltip("Controller for client socket")]
    public EGS_CL_Sockets clientSocketController = null;

    [Tooltip("In Game Sender for the Player Data")]
    private EGS_CL_InGameSender inGameSender = null;


    [Header("Client Data")]
    [Tooltip("Instance of the EGS User for this client")]
    private EGS_User user;

    [Tooltip("Long that stores the time a RTT lasts since server ask and receive")]
    private long clientPing;


    [Header("Game Data")]
    [Tooltip("Data about the Game Found")]
    private EGS_GameFoundData gameFoundData = null;

    [Tooltip("Object that stores all needed data about the game")]
    private EGS_UpdateData gameData = null;

    [Tooltip("Data about the Game Ended")]
    private EGS_GameEndData gameEndData = null;
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

            // Check if client is already connecting to the server.
            if (connectingToServer)
            {
                Debug.LogWarning("Client already connecting.");
            }
            return;
        }

        // Establish that is currently connecting to the server.
        connectingToServer = true;
        
        // Create the user.
        user = new EGS_User();
        EGS_ClientDelegates.onUserCreate?.Invoke(user);

        // Read server config data.
        ReadServerData();

        // Assign the current connection tries.
        currentConnectionTries = EGS_Config.CONNECTION_TRIES;

        // Create client socket manager.
        clientSocketController = new EGS_CL_Sockets();
        // Connect to the server.
        clientSocketController.ConnectToServer();
    }

    /// <summary>
    /// Method TryConnectToServerAgain, that will try to connect to the server up to EGS_CONFIG.CONNECTION_TRIES.
    /// </summary>
    public void TryConnectToServerAgain()
    {
        // Substract one unit from current connection tries.
        currentConnectionTries--;

        // If there are still connection tries.
        if (currentConnectionTries > 0)
        {
            // Connect to the server.
            clientSocketController.ConnectToServer();
        }
        else
        {
            // Reset the connection tries value.
            currentConnectionTries = EGS_Config.CONNECTION_TRIES;

            Debug.LogError("[CLIENT] Coulnd't connect to the server.");

            // Call the onCantConnectToServer delegate.
            EGS_ClientDelegates.onCantConnectToServer?.Invoke();
        }
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
        // Convert user to JSON.
        string userJson = JsonUtility.ToJson(user);

        // Send the message.
        SendMessage("QUEUE_JOIN", userJson);
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
        // Get debug mode.
        node = doc.DocumentElement.SelectSingleNode("//server/debug-mode");
        EGS_Config.DEBUG_MODE = int.Parse(node.InnerText);

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

        // Get connection tries.
        node = doc.DocumentElement.SelectSingleNode("//networking/connection-tries");
        EGS_Config.CONNECTION_TRIES = int.Parse(node.InnerText);

        /// Game Data.
        // Get the number of calculations per second.
        node = doc.DocumentElement.SelectSingleNode("//game/calculations-per-second");
        EGS_Config.CALCULATIONS_PER_SECOND = int.Parse(node.InnerText);
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
    /// Getter for the in game sender.
    /// </summary>
    /// <returns>In game sender</returns>
    public EGS_CL_InGameSender GetInGameSender()
    {
        return inGameSender;
    }

    /// <summary>
    /// Setter for the in game sender.
    /// </summary>
    /// <param name="g">New in game sender</param>
    public void SetInGameSender(EGS_CL_InGameSender igs)
    {
        inGameSender = igs;
    }

    /// <summary>
    /// Getter for the game found data.
    /// </summary>
    /// <returns>Game found data</returns>
    public EGS_GameFoundData GetGameFoundData()
    {
        return gameFoundData;
    }

    /// <summary>
    /// Setter for the game found data.
    /// </summary>
    /// <param name="g">New game found data</param>
    public void SetGameFoundData(EGS_GameFoundData g)
    {
        gameFoundData = g;
    }

    /// <summary>
    /// Getter for the game data.
    /// </summary>
    /// <returns>Game data</returns>
    public EGS_UpdateData GetGameData()
    {
        return gameData;
    }

    /// <summary>
    /// Setter for the game data.
    /// </summary>
    /// <param name="g">New game data</param>
    public void SetGameData(EGS_UpdateData g)
    {
        gameData = g;
    }

    /// <summary>
    /// Getter for the game end data.
    /// </summary>
    /// <returns>Game end data</returns>
    public EGS_GameEndData GetGameEndData()
    {
        return gameEndData;
    }

    /// <summary>
    /// Setter for the game end data.
    /// </summary>
    /// <param name="g">New game end data</param>
    public void SetGameEndData(EGS_GameEndData g)
    {
        gameEndData = g;
    }
    #endregion
}
