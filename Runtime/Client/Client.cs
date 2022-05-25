using System.IO;
using System.Xml;
using UnityEngine;

/// <summary>
/// Class Client, that controls the client side.
/// </summary>
public class Client : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static Client instance;
  

    [Header("Networking")]
    [Tooltip("Bool that indicates if client is connecting to a server")]
    private bool connectingToServer = false;

    [Tooltip("Bool that indicates if client is connected to the master server")]
    private bool connectedToMasterServer = false;

    [Tooltip("Bool that indicates if client is connected to the game server")]
    private bool connectedToGameServer = false;

    [Tooltip("Client socket manager")]
    private ClientSocketManager clientSocketManager = null;

    [Tooltip("Integer that indicates the number of current tries to connect to the server")]
    private int currentConnectionTries;

    [Tooltip("In Game Sender for the Player Data")]
    private ClientInGameSender inGameSender = null;


    [Header("Client Data")]
    [Tooltip("Instance of the UserData for this client")]
    private UserData user;

    [Tooltip("Long that stores the time a RTT lasts since server ask and receive")]
    private long clientPing;


    [Header("Game Data")]
    [Tooltip("Data about the Game Found")]
    private GameFoundData gameFoundData = null;

    [Tooltip("Object that stores all needed data about the game")]
    private UpdateData gameData = null;

    [Tooltip("Data about the Game Ended")]
    private GameEndData gameEndData = null;
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
    /// Method OnApplicationQuit, called to free resources when closing the application.
    /// </summary>
    private void OnApplicationQuit()
    {
        // If the in game sender is running.
        if (inGameSender != null && inGameSender.IsGameRunning())
            inGameSender.StopGameLoop();

        // If already created the client socket controller, close the socket.
        if (clientSocketManager != null)
            clientSocketManager.CloseSocketOnApplicationQuit();
    }
    #endregion

    #region Class Methods
    #region Connections
    /// <summary>
    /// Method ConnectToServer, that prepares all client data and try to connect to the server.
    /// </summary>
    public void ConnectToServer()
    {
        // Check if server already started.
        if (connectedToMasterServer)
        {
            Debug.LogWarning("[EGS_CLIENT] Client already connected.");

            // Check if client is already connecting to the server.
            if (connectingToServer)
            {
                Debug.LogWarning("[EGS_CLIENT] Client already connecting.");
            }
            return;
        }

        // Establish that is currently connecting to the server.
        connectingToServer = true;
        
        // Create the user.
        user = new UserData();

        // Call the onUserCreate delegate.
        ClientDelegates.onUserCreate?.Invoke(user);

        // Read server config data.
        ReadServerData();

        // Assign the current connection tries.
        currentConnectionTries = EasyGameServerConfig.CONNECTION_TRIES;

        // Create client socket manager.
        clientSocketManager = new ClientSocketManager();

        // Connect to the server.
        clientSocketManager.ConnectToServer();
    }

    /// <summary>
    /// Method TryConnectToServerAgain, that will try to connect to the server up to EGS_CONFIG.CONNECTION_TRIES times.
    /// </summary>
    public void TryConnectToServerAgain()
    {
        // Substract one unit from current connection tries.
        currentConnectionTries--;

        // If there are still connection tries.
        if (currentConnectionTries > 0)
        {
            // Connect to the server.
            clientSocketManager.ConnectToServer();
        }
        else
        {
            // Reset the connection tries value.
            currentConnectionTries = EasyGameServerConfig.CONNECTION_TRIES;

            Debug.LogError("[EGS_CLIENT] Coulnd't connect to the server.");

            // Call the onCantConnectToMasterServer delegate.
            ClientDelegates.onCantConnectToServer?.Invoke();
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

        // DisconnectFromMasterServer from server.
        clientSocketManager.DisconnectFromServer();
    }

    /// <summary>
    /// Method DeleteUser, to delete the Client user from the Server.
    /// </summary>
    public void DeleteUser()
    {
        // Convert user to JSON.
        string userJson = JsonUtility.ToJson(user);

        // Send the message.
        SendMessage(MasterServerMessageTypes.USER_DELETE, userJson);
    }
    #endregion

    #region Messaging
    /// <summary>
    /// Method SendMessage, that will send a message to the server
    /// </summary>
    /// <param name="type">String that contains the type of the message</param>
    /// <param name="msg">String that contains the message itself</param>
    public void SendMessage(string type, string msg)
    {
        // Send the message by the socket controller.
        clientSocketManager.SendMessage(type, msg);
    }

    /// <summary>
    /// Method SendMessage, that will send a message to the server
    /// </summary>
    /// <param name="messageToSend">Message to send to the server</param>
    public void SendMessage(NetworkMessage messageToSend)
    {
        // Send the message by the socket controller.
        clientSocketManager.SendMessage(messageToSend);
    }
    #endregion

    #region Moments
    /// <summary>
    /// Method JoinQueue, that will ask the server for a game.
    /// </summary>
    public void JoinQueue()
    {
        // Convert user to JSON.
        string userJson = JsonUtility.ToJson(user);

        // Send the message.
        SendMessage(MasterServerMessageTypes.QUEUE_JOIN, userJson);
    }

    /// <summary>
    /// Method LeaveQueue, to stop searching a game.
    /// </summary>
    public void LeaveQueue()
    {
        SendMessage(MasterServerMessageTypes.QUEUE_LEAVE, "");
    }

    /// <summary>
    /// Method LeaveGame, that sends a message to the server (must be the Game Server) to ask for leave the current game.
    /// </summary>
    public void LeaveGame()
    {
        clientSocketManager.SendMessage(GameServerMessageTypes.LEAVE_GAME, user.GetIngameID().ToString());
    }
    #endregion

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
        node = doc.DocumentElement.SelectSingleNode("//server/debug-mode-console");
        EasyGameServerConfig.DEBUG_MODE_CONSOLE = (EasyGameServerControl.EnumLogDebugLevel)int.Parse(node.InnerText);

        // Get time between round trip times.
        node = doc.DocumentElement.SelectSingleNode("//server/time-between-rtt");
        EasyGameServerConfig.TIME_BETWEEN_RTTS = int.Parse(node.InnerText);

        /// Networking Data.
        // Get server ip.
        node = doc.DocumentElement.SelectSingleNode("//networking/server-ip");
        EasyGameServerConfig.SERVER_IP = node.InnerText;

        // Get server port.
        node = doc.DocumentElement.SelectSingleNode("//networking/base-port");
        EasyGameServerConfig.SERVER_PORT = int.Parse(node.InnerText);

        // Get connection tries.
        node = doc.DocumentElement.SelectSingleNode("//networking/connection-tries");
        EasyGameServerConfig.CONNECTION_TRIES = int.Parse(node.InnerText);

        /// Game Data.
        // Get the number of calculations per second.
        node = doc.DocumentElement.SelectSingleNode("//game/calculations-per-second");
        EasyGameServerConfig.CALCULATIONS_PER_SECOND = int.Parse(node.InnerText);
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the connecting to server bool.
    /// </summary>
    /// <returns>Bool that indicates if client is connecting to a server</returns>
    public bool GetConnectingToServer() { return connectingToServer; }

    /// <summary>
    /// Setter for the connecting to server bool.
    /// </summary>
    /// <param name="connectingToServer">New bool that indicates if client is connecting to a server</param>
    public void SetConnectingToServer(bool connectingToServer) { this.connectingToServer = connectingToServer; }

    /// <summary>
    /// Getter for the connected to master server bool.
    /// </summary>
    /// <returns>Bool that indicates if client is connected to the master server</returns>
    public bool GetConnectedToMasterServer() { return connectedToMasterServer; }

    /// <summary>
    /// Setter for the connected to master server bool.
    /// </summary>
    /// <param name="connectedToMasterServer">New bool that indicates if client is connected to the master server</param>
    public void SetConnectedToMasterServer(bool connectedToMasterServer) { this.connectedToMasterServer = connectedToMasterServer; }

    /// <summary>
    /// Getter for the connected to game server bool.
    /// </summary>
    /// <returns>Bool that indicates if client is connected to a game server</returns>
    public bool GetConnectedToGameServer() { return connectedToGameServer; }

    /// <summary>
    /// Setter for the connected to game server bool.
    /// </summary>
    /// <param name="connectedToGameServer">New bool that indicates if client is connected to a game server</param>
    public void SetConnectedToGameServer(bool connectedToGameServer) { this.connectedToGameServer = connectedToGameServer; }

    /// <summary>
    /// Getter for the client socket controller.
    /// </summary>
    /// <returns>Instance of the client socket controller</returns>
    public ClientSocketManager GetClientSocketsController() { return clientSocketManager; }

    /// <summary>
    /// Setter for the client socket controller.
    /// </summary>
    /// <param name="clientSocketController">New instance of the client socket controller</param>
    public void SetClientSocketController(ClientSocketManager clientSocketController) { this.clientSocketManager = clientSocketController; }

    /// <summary>
    /// Getter for the client user.
    /// </summary>
    /// <returns>Client user</returns>
    public UserData GetUser() { return user; }

    /// <summary>
    /// Setter for the client user.
    /// </summary>
    /// <param name="user">New client user</param>
    public void SetUser(UserData user) { this.user = user; }

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

    /// <summary>
    /// Getter for the in game sender.
    /// </summary>
    /// <returns>In game sender</returns>
    public ClientInGameSender GetInGameSender() { return inGameSender; }

    /// <summary>
    /// Setter for the in game sender.
    /// </summary>
    /// <param name="inGameSender">New in game sender</param>
    public void SetInGameSender(ClientInGameSender inGameSender) { this.inGameSender = inGameSender; }

    /// <summary>
    /// Getter for the game found data.
    /// </summary>
    /// <returns>Game found data</returns>
    public GameFoundData GetGameFoundData() { return gameFoundData;  }

    /// <summary>
    /// Setter for the game found data.
    /// </summary>
    /// <param name="gameFoundData">New game found data</param>
    public void SetGameFoundData(GameFoundData gameFoundData) { this.gameFoundData = gameFoundData; }

    /// <summary>
    /// Getter for the game data.
    /// </summary>
    /// <returns>Game data</returns>
    public UpdateData GetGameData() { return gameData; }

    /// <summary>
    /// Setter for the game data.
    /// </summary>
    /// <param name="gameData">New game data</param>
    public void SetGameData(UpdateData gameData) { this.gameData = gameData; }

    /// <summary>
    /// Getter for the game end data.
    /// </summary>
    /// <returns>Game end data</returns>
    public GameEndData GetGameEndData() { return gameEndData; }

    /// <summary>
    /// Setter for the game end data.
    /// </summary>
    /// <param name="gameEndData">New game end data</param>
    public void SetGameEndData(GameEndData gameEndData) { this.gameEndData = gameEndData; }
    #endregion
}
