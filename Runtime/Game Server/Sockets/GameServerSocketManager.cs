using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class GameServerSocketManager, that controls both sockets of the game server (SERVER and CLIENT).
/// </summary>
public class GameServerSocketManager
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Client Socket")]
    private Socket socket_client;

    [Tooltip("Instance of the handler for the client socket")]
    private GameServerClientSocketHandler clientSocketHandler;

    [Tooltip("Server Socket")]
    private Socket socket_server;

    [Tooltip("Handler for the server socket")]
    private GameServerServerSocketHandler serverSocketHandler;

    [Tooltip("ManualResetEvent for when game server can handle player connections")]
    private ManualResetEvent startDone_MRE = new ManualResetEvent(false);


    [Header("Threading")]
    [Tooltip("Thread that handles the client connections")]
    private Thread clientConnectionsThread;

    [Tooltip("Thread that handles the server connections")]
    private Thread serverConnectionsThread;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public GameServerSocketManager()
    {

    }
    #endregion

    #region Class Methods
    #region Connections
    /// <summary>
    /// Method ConnectToMasterServer, to establish a connection to the master server.
    /// </summary>
    public void ConnectToMasterServer()
    {
        // Create socket and get EndPoint.
        EndPoint remoteEP = CreateClientSocket();

        // Connect to server.
        clientSocketHandler = new GameServerClientSocketHandler(this);
        clientConnectionsThread = new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client));
        clientConnectionsThread.Start();
    }

    /// <summary>
    /// Method DisconnectFromMasterServer, to stop the client thread and disconnect from server.
    /// </summary>
    public void DisconnectFromMasterServer()
    {
        // Close the Client socket.
        CloseClientSocket();
    }

    /// <summary>
    /// Method StartListening, to start listeting connections from clients.
    /// </summary>
    public void StartListening()
    {
        // Create socket and get EndPoint.
        EndPoint localEP = CreateServerSocket(GameServer.instance.GetGameServerPort());

        // Connect to server.
        serverSocketHandler = new GameServerServerSocketHandler(this);
        serverConnectionsThread = new Thread(() => serverSocketHandler.StartListening(localEP, socket_server, EasyGameServerConfig.PLAYERS_PER_GAME));
        serverConnectionsThread.Start();

        // Create the Game.
        CreateGame();
    }

    /// <summary>
    /// Method StopListening, to close the server socket and stop listening to connections.
    /// </summary>
    public void StopListening()
    {
        // Close the Server Socket.
        CloseServerSocket();
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
        // Create new message.
        NetworkMessage thisMessage = new NetworkMessage(messageType, messageContent);

        // Convert message to JSON.
        string messageJson = thisMessage.ConvertMessage();

        // Send the message.
        clientSocketHandler.Send(socket_client, messageJson);
    }

    /// <summary>
    /// Method SendMessageToMasterServer, that will send the NetworkMessage given.
    /// </summary>
    /// <param name="messageToSend">NetworkMessage to send</param>
    public void SendMessageToMasterServer(NetworkMessage messageToSend)
    {
        // Convert message to JSON.
        string messageJson = messageToSend.ConvertMessage();

        // Send the message.
        clientSocketHandler.Send(socket_client, messageJson);
    }

    /// <summary>
    /// Method SendMessageToClient, that will send a message to a client.
    /// </summary>
    /// <param name="socket">Socket to send the message</param>
    /// <param name="messageType">String that contains the type of the message</param>
    /// <param name="messageContent">String that contains the message itself</param>
    public void SendMessageToClient(Socket socket, string messageType, string messageContent)
    {
        // Create new message.
        NetworkMessage thisMessage = new NetworkMessage(messageType, messageContent);

        // Convert message to JSON .
        string messageJson = thisMessage.ConvertMessage();

        // Send the message.
        serverSocketHandler.Send(socket, messageJson);
    }

    /// <summary>
    /// Method SendMessageToClient, that will send a message to a client.
    /// </summary>
    /// <param name="socket">Socket to send the message</param>
    /// <param name="messageToSend">NetworkMessage to send</param>
    public void SendMessageToClient(Socket socket, NetworkMessage messageToSend)
    {
        // Convert message to JSON .
        string messageJson = messageToSend.ConvertMessage();

        // Send the message.
        serverSocketHandler.Send(socket, messageJson);
    }
    #endregion

    #region Sockets
    /// <summary>
    /// Method CreateClientSocket, that creates the client socket and returns the server endpoint.
    /// </summary>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateClientSocket()
    {
        // Obtain IP address.
        IPAddress ipAddress;
        IPAddress.TryParse(EasyGameServerConfig.SERVER_IP, out ipAddress);

        IPEndPoint remoteEP = new IPEndPoint(ipAddress, EasyGameServerConfig.SERVER_PORT);

        // Create a TCP/IP socket
        socket_client = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

        // Return the EndPoint
        return remoteEP;
    }

    /// <summary>
    /// Method CreateServerSocket, that creates the server socket and returns the endpoint.
    /// </summary>
    /// <param name="gameServerPort">Port where the game server will be set</param>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateServerSocket(int gameServerPort)
    {
        IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
        IPAddress ipAddress = ips[1]; // IPv4

        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, gameServerPort);

        // Create a TCP/IP socket
        socket_server = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

        // Return the EndPoint
        return localEndPoint;
    }

    /// <summary>
    /// Method CloseClientSocket, to close the client socket.
    /// </summary>
    public void CloseClientSocket()
    {
        socket_client.Shutdown(SocketShutdown.Both);
        socket_client.Close();

        // Log on the Game Server.
        //Debug.Log("[GameServer] Closed client socket.");
    }

    /// <summary>
    /// Method CloseServerSocket, to close the client socket.
    /// </summary>
    public void CloseServerSocket()
    {
        socket_server.Shutdown(SocketShutdown.Both);
        socket_server.Close();

        // Log on the Game Server.
        //Debug.Log("[GameServer] Closed server socket.");
    }

    /// <summary>
    /// Method CloseSocketOnApplicationQuit, to close the client socket when closing the application.
    /// </summary>
    public void CloseSocketsOnApplicationQuit()
    {
        try
        {
            clientConnectionsThread.Interrupt();
            serverConnectionsThread.Interrupt();

            if (socket_client.Connected)
                CloseClientSocket();

            if (socket_server.Connected)
                CloseServerSocket();
        }
        catch (SocketException se)
        {
            // LOG.
            Debug.LogError("[GAME_SERVER] SocketException: " + se.ToString());
        }
        catch (ObjectDisposedException ode)
        {
            // LOG.
            Debug.LogError("[GAME_SERVER] ObjectDisposedException: " + ode.ToString());
        }
    }
    #endregion

    #region Other
    /// <summary>
    /// Method GetPlayersConnected, to know how many players are connected to the Game Server.
    /// </summary>
    /// <returns>Number of players connected to the Game Server</returns>
    public int GetPlayersConnected()
    {
        return serverSocketHandler.GetConnectedUsers().Count;
    }

    /// <summary>
    /// Method CreateGame, to create the game instance.
    /// </summary>
    private void CreateGame()
    {
        // Create the game data.
        GameServer.instance.SetGame(new Game(serverSocketHandler, GameServer.instance.GetGameFoundData().GetRoom(), "Level_0"));
        // TODO: Get the scene name from the server.
    }
    #endregion
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the StartDone ManualResetEvent.
    /// </summary>
    /// <returns>StartDone ManualResetEvent</returns>
    public ManualResetEvent GetStartDoneMRE() { return startDone_MRE; }

    /// <summary>
    /// Setter for the StartDone ManualResetEvent.
    /// </summary>
    /// <param name="startDone_MRE">New StartDone ManualResetEvent</param>
    public void SetStartDoneMRE(ManualResetEvent startDone_MRE) { this.startDone_MRE = startDone_MRE; }

    /// <summary>
    /// Getter for the ClientConnectionsThread.
    /// </summary>
    /// <returns>Client Connections Thread</returns>
    public Thread GetClientConnectionsThread() { return clientConnectionsThread; }

    /// <summary>
    /// Setter for the ClientConnectionsThread.
    /// </summary>
    /// <param name="clientConnectionsThread">New Client Connections Thread</param>
    public void SetClientConnectionsThread(Thread clientConnectionsThread) { this.clientConnectionsThread = clientConnectionsThread; }
    #endregion
}
