using System;
using System.IO;
using System.Net.Sockets;
using System.Xml;
using UnityEngine;

/// <summary>
/// Class EGS_GameServer, that controls the game server side.
/// </summary>
public class EGS_GameServer : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_GameServer instance;

    [Tooltip("Struct that contains the server data")]
    public EGS_Config serverData;

    [Tooltip("Integer that indicates the number of current tries to connect to the server")]
    private int currentConnectionTries;


    [Header("Networking")]
    [Tooltip("Port where the Game Server will be hosted")]
    public int gameServerPort;

    [Tooltip("Controller for game server sockets")]
    public EGS_GS_Sockets gameServerSocketsController = null;

    [Tooltip("Bool that indicates if game server is conected to the master server")]
    public bool connectedToMasterServer;


    [Header("Game Server Data")] // TODO: Save as an EGS_GameServerData object.
    [Tooltip("Game Server State")]
    public EGS_GameServerData.EGS_GameServerState gameServerState;

    [Tooltip("Game Server ID")]
    public int gameServerID = -1;

    [Tooltip("Instance of the game")]
    public EGS_Game thisGame;

    [Tooltip("Game found data, that is received on parameters")]
    public EGS_GameFoundData gameFoundData; // TODO: Think if need to store.

    [Tooltip("Data about the Game Ended")]
    private EGS_GameEndData gameEndData = null;

    [Tooltip("Long that stores the time a RTT lasts since server ask and receive")]
    private long clientPing;

    // TEST .
    // TODO: Make a Game Server Console with Log and UI.
    public TMPro.TextMeshProUGUI test_text;

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

    /// <summary>
    /// Method Start, executed before the first frame.
    /// </summary>
    private void Start()
    {
        try
        {
            ReadArguments();
            ReadConfigData(); // TODO: Read Config Data from Master Server as a JSON Resource.

            // Change the server state.
            gameServerState = EGS_GameServerData.EGS_GameServerState.CREATED;
            EGS_Dispatcher.RunOnMainThread(() => { test_text.text += "\nStatus: " + Enum.GetName(typeof(EGS_GameServerData.EGS_GameServerState), gameServerState); });

            // Call the onGameServerCreated delegate.
            EGS_GameServerDelegates.onGameServerCreated?.Invoke();

            // Connect to the master server.
            ConnectToMasterServer();
        }
        catch (Exception e) {
            EGS_Dispatcher.RunOnMainThread(() => { test_text.text += "\nERROR: " + e.ToString(); });
        }

    }
    #endregion

    #region Class Methods
    #region Public Methods
    /// <summary>
    /// Method SendMessage, that will send a message to the server
    /// </summary>
    /// <param name="type">String that contains the type of the message</param>
    /// <param name="socket">Socket to send the message</param>
    /// <param name="msg">String that contains the message itself</param>
    public void SendMessageToClient(Socket socket, string type, string msg)
    {
        // Send the message by the socket controller.
        gameServerSocketsController.SendMessageToClient(socket, type, msg);
    }

    /// <summary>
    /// Method SendMessage, that will send a message to the server
    /// </summary>
    /// <param name="socket">Socket to send the message</param>
    /// <param name="messageToSend">Message to send to the server</param>
    public void SendMessageToClient(Socket socket, EGS_Message messageToSend)
    {
        // Send the message by the socket controller.
        gameServerSocketsController.SendMessageToClient(socket, messageToSend);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method ConnectToMasterServer, that tries to connect to the server.
    /// </summary>
    private void ConnectToMasterServer()
    {
        // Create sockets manager.
        gameServerSocketsController = new EGS_GS_Sockets();

        // Connect to the server.
        gameServerSocketsController.ConnectToMasterServer();
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
            gameServerSocketsController.ConnectToMasterServer();
        }
        else
        {
            // Reset the connection tries value.
            currentConnectionTries = EGS_Config.CONNECTION_TRIES;

            // LOG.
            //Debug.Log("[GAME SERVER] Coulnd't connect to the server.");

            // Call the onCantConnectToServer delegate.
            EGS_GameServerDelegates.onCantConnectToServer?.Invoke();
        }
    }

    /// <summary>
    /// Method DisconnectFromMasterServer, that will stop sending messages and listening.
    /// </summary>
    private void DisconnectFromMasterServer()
    {
        // Stop listening on the sockets.
        gameServerSocketsController.Disconnect();
    }

    /// <summary>
    /// Method ReadArguments, to load the received arguments.
    /// </summary>
    private void ReadArguments()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        string[] realArguments = arguments[1].Split('#');
        EGS_Config.serverIP = realArguments[0];
        EGS_Config.serverPort = int.Parse(realArguments[1]);
        gameServerID = int.Parse(realArguments[2]);
        gameServerPort = int.Parse(realArguments[3]);
    }

    /// <summary>
    /// Method ReadConfigData, to load all server config data.
    /// </summary>
    private void ReadConfigData()
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

        // Get time to disconnect client if no response.
        node = doc.DocumentElement.SelectSingleNode("//server/disconnect-timeout");
        EGS_Config.DISCONNECT_TIMEOUT = int.Parse(node.InnerText);

        /// Games Data.
        // Get the number of players per game.
        node = doc.DocumentElement.SelectSingleNode("//game/players-per-game"); // TODO: Make MIN_PLAYERS and MAX_PLAYERS.
        EGS_Config.PLAYERS_PER_GAME = int.Parse(node.InnerText);

        // Get the number of calculations per second.
        node = doc.DocumentElement.SelectSingleNode("//game/calculations-per-second");
        EGS_Config.CALCULATIONS_PER_SECOND = int.Parse(node.InnerText);
    }

    #region Getters and setters
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
    #endregion
    #endregion
}
