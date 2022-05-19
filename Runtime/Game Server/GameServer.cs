using System;
using System.IO;
using System.Net.Sockets;
using System.Xml;
using UnityEngine;
using TMPro;

/// <summary>
/// Class GameServer, that controls the game server side.
/// </summary>
public class GameServer : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static GameServer instance;


    [Header("Networking")]
    [Tooltip("Port where the Game Server will be hosted")]
    private int gameServerPort;

    [Tooltip("Controller for game server sockets")]
    private GameServerSocketManager gameServerSocketsController = null;

    [Tooltip("Bool that indicates if game server is conected to the master server")]
    private bool connectedToMasterServer;

    [Tooltip("Integer that indicates the number of current tries to connect to the server")]
    private int currentConnectionTries;


    [Header("Game Server Data")]
    [Tooltip("Game Server State")]
    private GameServerData.EnumGameServerState gameServerState;

    [Tooltip("Game Server ID")]
    private int gameServerID = -1;

    [Tooltip("Instance of the game")]
    private Game game;

    [Tooltip("Game found data, that is received on parameters")]
    private GameFoundData gameFoundData; // TODO: Think if need to store.

    [Tooltip("Data about the Game Ended")]
    private GameEndData gameEndData = null;

    [Tooltip("Long that stores the time a RTT lasts since server ask and receive")]
    private long clientPing;

    // TEST .
    // TODO: Make a Game Server Console with Log and UI.
    [Header("Control")]
    [Tooltip("Text shown in the Game Server Console")]
    public TextMeshProUGUI console_text;
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, executed on script load.
    /// </summary>
    private void Awake()
    {
        // Instantiate the singleton.
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
            // Read the arguments and the config data.
            ReadArguments();
            ReadConfigData(); // TODO: Read Config Data from Master Server as a JSON Resource.

            // Assign the current connection tries.
            currentConnectionTries = EasyGameServerConfig.CONNECTION_TRIES;

            // Change the server state.
            gameServerState = GameServerData.EnumGameServerState.CREATED; // LOG.
            MainThreadDispatcher.RunOnMainThread(() => { console_text.text += "\nStatus: " + Enum.GetName(typeof(GameServerData.EnumGameServerState), gameServerState); });

            // Call the onGameServerCreated delegate.
            GameServerDelegates.onGameServerCreated?.Invoke();

            // Connect to the master server.
            ConnectToMasterServer();
        }
        catch (Exception e) {
            // LOG.
            MainThreadDispatcher.RunOnMainThread(() => { console_text.text += "\nERROR: " + e.ToString(); });
        }
    }

    /// <summary>
    /// Method OnApplicationQuit, called to free resources when closing the application.
    /// </summary>
    private void OnApplicationQuit()
    {
        // If already created the sockets controller, interrupt the threads and close the sockets.
        if (gameServerSocketsController != null)
        {
            gameServerSocketsController.CloseSocketsOnApplicationQuit();
        }
    }
    #endregion

    #region Class Methods
    #region Connections
    /// <summary>
    /// Method ConnectToMasterServer, that tries to connect to the server.
    /// </summary>
    private void ConnectToMasterServer()
    {
        // Create sockets manager.
        gameServerSocketsController = new GameServerSocketManager();

        // Connect to the server.
        gameServerSocketsController.ConnectToMasterServer();
    }

    /// <summary>
    /// Method TryConnectToServerAgain, that will try to connect to the master server up to EGS_CONFIG.CONNECTION_TRIES.
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
            currentConnectionTries = EasyGameServerConfig.CONNECTION_TRIES;

            // LOG.
            //Debug.Log("[GAME SERVER] Coulnd't connect to the server.");

            // Call the onCantConnectToServer delegate.
            GameServerDelegates.onCantConnectToServer?.Invoke();
        }
    }

    /// <summary>
    /// Method DisconnectFromMasterServer, that will stop sending messages and listening.
    /// </summary>
    public void DisconnectFromMasterServer() // TODO: Why use?
    {
        // If not connected to master server, return.
        if (!connectedToMasterServer)
            return;

        // Stop listening on the sockets.
        gameServerSocketsController.DisconnectFromMasterServer();
    }
    #endregion

    #region Messaging
    /// <summary>
    /// Method SendMessageToMasterServer, that will send a NetworkMessage with the given type and content.
    /// </summary>
    /// <param name="messageType">String that contains the type of the message</param>
    /// <param name="messageContent">String that contains the message itself</param>
    public void SendMessageToMasterServer(string messageType, string messageContent)
    {
        // Send the message by the socket controller.
        gameServerSocketsController.SendMessageToMasterServer(messageType, messageContent);
    }

    /// <summary>
    /// Method SendMessageToMasterServer, that will send the NetworkMessage given.
    /// </summary>
    /// <param name="messageToSend">NetworkMessage to send</param>
    public void SendMessageToMasterServer(NetworkMessage messageToSend)
    {
        // Send the message by the socket controller.
        gameServerSocketsController.SendMessageToMasterServer(messageToSend);
    }

    /// <summary>
    /// Method SendMessageToClient, that will send a message to a client.
    /// </summary>
    /// <param name="socket">Socket to send the message</param>
    /// <param name="messageType">String that contains the type of the message</param>
    /// <param name="messageContent">String that contains the message itself</param>
    public void SendMessageToClient(Socket socket, string messageType, string messageContent)
    {
        // Send the message by the socket controller.
        gameServerSocketsController.SendMessageToClient(socket, messageType, messageContent);
    }

    /// <summary>
    /// Method SendMessageToClient, that will send a message to a client.
    /// </summary>
    /// <param name="socket">Socket to send the message</param>
    /// <param name="messageToSend">NetworkMessage to send</param>
    public void SendMessageToClient(Socket socket, NetworkMessage messageToSend)
    {
        // Send the message by the socket controller.
        gameServerSocketsController.SendMessageToClient(socket, messageToSend);
    }
    #endregion

    /// <summary>
    /// Method ReadArguments, to load the received arguments.
    /// </summary>
    private void ReadArguments()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        string[] realArguments = arguments[1].Split('#');
        EasyGameServerConfig.SERVER_IP = realArguments[0];
        EasyGameServerConfig.SERVER_PORT = int.Parse(realArguments[1]);
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
        node = doc.DocumentElement.SelectSingleNode("//server/debug-mode-console");
        EasyGameServerConfig.DEBUG_MODE_CONSOLE = (EasyGameServerControl.EnumLogDebugLevel)int.Parse(node.InnerText);

        node = doc.DocumentElement.SelectSingleNode("//server/debug-mode-file");
        EasyGameServerConfig.DEBUG_MODE_FILE = (EasyGameServerControl.EnumLogDebugLevel)int.Parse(node.InnerText);

        // Get time between round trip times.
        node = doc.DocumentElement.SelectSingleNode("//server/time-between-rtt");
        EasyGameServerConfig.TIME_BETWEEN_RTTS = int.Parse(node.InnerText);

        // Get time to disconnect client if no response.
        node = doc.DocumentElement.SelectSingleNode("//server/disconnect-timeout");
        EasyGameServerConfig.DISCONNECT_TIMEOUT = int.Parse(node.InnerText);

        /// Games Data.
        // Get the number of players per game.
        node = doc.DocumentElement.SelectSingleNode("//game/players-per-game"); // TODO: Make MIN_PLAYERS and MAX_PLAYERS.
        EasyGameServerConfig.PLAYERS_PER_GAME = int.Parse(node.InnerText);

        // Get the number of calculations per second.
        node = doc.DocumentElement.SelectSingleNode("//game/calculations-per-second");
        EasyGameServerConfig.CALCULATIONS_PER_SECOND = int.Parse(node.InnerText);
    }
    #endregion

    #region Getters and setters
    /// <summary>
    /// Getter for the Game Server port.
    /// </summary>
    /// <returns>Port where the Game Server must be hosted</returns>
    public int GetGameServerPort() { return gameServerPort; }

    /// <summary>
    /// Setter for the Game Server port.
    /// </summary>
    /// <param name="gameServerPort">New port where the Game Server must be hosted</param>
    public void SetGameServerPort(int gameServerPort) { this.gameServerPort = gameServerPort; }

    /// <summary>
    /// Getter for the Game Server Sockets Controller.
    /// </summary>
    /// <returns>Game Server Sockets Controller</returns>
    public GameServerSocketManager GetGameServerSocketsController() { return gameServerSocketsController; }

    /// <summary>
    /// Setter for the Game Server Sockets Controller.
    /// </summary>
    /// <param name="gameServerSocketsController">New Game Server Sockets Controller</param>
    public void SetGameServerSocketsController(GameServerSocketManager gameServerSocketsController) { this.gameServerSocketsController = gameServerSocketsController; }

    /// <summary>
    /// Getter for the Connected To Master Server bool.
    /// </summary>
    /// <returns>Bool indicating if the Game Server is connected to the Master Server</returns>
    public bool IsConnectedToMasterServer() { return connectedToMasterServer; }

    /// <summary>
    /// Setter for the Connected To Master Server bool.
    /// </summary>
    /// <param name="connectedToMasterServer">New bool indicating if the Game Server is connected to the Master Server</param>
    public void SetConnectedToMasterServer(bool connectedToMasterServer) { this.connectedToMasterServer = connectedToMasterServer; }

    /// <summary>
    /// Getter for the Game Server status.
    /// </summary>
    /// <returns>Game Server status</returns>
    public GameServerData.EnumGameServerState GetGameServerState() { return gameServerState; }

    /// <summary>
    /// Setter for the Game Server status.
    /// </summary>
    /// <param name="gameServerPort">New Game Server status</param>
    public void SetGameServerState(GameServerData.EnumGameServerState gameServerState) { this.gameServerState = gameServerState; }

    /// <summary>
    /// Getter for the Game Server ID.
    /// </summary>
    /// <returns>ID from this Game Server</returns>
    public int GetGameServerID() { return gameServerID; }

    /// <summary>
    /// Setter for the Game Server ID.
    /// </summary>
    /// <param name="gameServerID">New ID for this Game Server</param>
    public void SetGameServerID(int gameServerID) { this.gameServerID = gameServerID; }

    /// <summary>
    /// Getter for the Game.
    /// </summary>
    /// <returns>Game on this Game Server</returns>
    public Game GetGame() { return game; }

    /// <summary>
    /// Setter for the Game.
    /// </summary>
    /// <param name="game">New game to this Game Server</param>
    public void SetGame(Game game) { this.game = game; }

    /// <summary>
    /// Getter for the Game Found Data.
    /// </summary>
    /// <returns>Game found data</returns>
    public GameFoundData GetGameFoundData() { return gameFoundData; }

    /// <summary>
    /// Setter for the Game Found Data.
    /// </summary>
    /// <param name="gameFoundData">New game found data</param>
    public void SetGameFoundData(GameFoundData gameFoundData) { this.gameFoundData = gameFoundData; }

    /// <summary>
    /// Getter for the Game End Data.
    /// </summary>
    /// <returns>Game end data</returns>
    public GameEndData GetGameEndData() { return gameEndData; }

    /// <summary>
    /// Setter for the Game End Data.
    /// </summary>
    /// <param name="gameEndData">New game end data</param>
    public void SetGameEndData(GameEndData gameEndData) { this.gameEndData = gameEndData; }

    /// <summary>
    /// Getter for the client ping.
    /// </summary>
    /// <returns>Client ping</returns>
    public long GetClientPing() { return clientPing; }

    /// <summary>
    /// Setter for the client ping.
    /// </summary>
    /// <param name="clientPing">New client ping</param>
    public void SetClientPing(long clientPing) { this.clientPing = clientPing; }
    #endregion
}
