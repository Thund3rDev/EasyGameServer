using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_GS_Sockets, that controls both sockets of the game server (SERVER and CLIENT).
/// </summary>
public class EGS_GS_Sockets
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Client Socket")]
    private Socket socket_client;

    [Tooltip("Instance of the handler for the client socket")]
    public EGS_GS_ClientSocket clientSocketHandler;

    [Tooltip("Server Socket")]
    private Socket socket_server;

    [Tooltip("Handler for the server socket")]
    private EGS_GS_ServerSocket serverSocketHandler;

    [Tooltip("EndPoint to the game server")]
    public EndPoint localEP;

    [Tooltip("ManualResetEvent for when game server can handle player connections")]
    public ManualResetEvent startDone = new ManualResetEvent(false);

    [Tooltip("Thread that handles the client connections")]
    public Thread clientConnectionsThread;

    [Tooltip("Thread that handles the server connections")]
    public Thread serverConnectionsThread;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_GS_Sockets()
    {

    }
    #endregion

    #region Class Methods
    #region Public Methods
    /// <summary>
    /// Method ConnectToMasterServer, to establish a connection to the master server.
    /// </summary>
    public void ConnectToMasterServer()
    {
        // Create socket and get EndPoint.
        EndPoint remoteEP = CreateSocket(EGS_Config.serverIP, EGS_Config.serverPort);

        // Connect to server.
        clientSocketHandler = new EGS_GS_ClientSocket(this);
        clientConnectionsThread = new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client));
        clientConnectionsThread.Start();
    }

    /// <summary>
    /// Method StartListening, to start listeting connections from clients.
    /// </summary>
    public void StartListening()
    {
        // Create socket and get EndPoint.
        localEP = CreateGameServerSocket(EGS_GameServer.instance.gameServerPort);

        // Connect to server.
        serverSocketHandler = new EGS_GS_ServerSocket(this);
        serverConnectionsThread = new Thread(() => serverSocketHandler.StartListening(localEP, socket_server, EGS_Config.PLAYERS_PER_GAME));
        serverConnectionsThread.Start();

        // Create the Game.
        CreateGame();
    }

    public void SendMessageToMasterServer(string type, string msg)
    {
        // Create new message.
        EGS_Message thisMessage = new EGS_Message(type, msg);

        // Convert message to JSON.
        string messageJson = thisMessage.ConvertMessage();

        // Send the message.
        clientSocketHandler.Send(socket_client, messageJson);
    }

    public void SendMessageToMasterServer(EGS_Message messageToSend)
    {
        // Convert message to JSON.
        string messageJson = messageToSend.ConvertMessage();

        // Send the message.
        clientSocketHandler.Send(socket_client, messageJson);
    }   

    public void SendMessageToClient(Socket socket, string type, string msg)
    {
        // Create new message.
        EGS_Message thisMessage = new EGS_Message(type, msg);

        // Convert message to JSON .
        string messageJson = thisMessage.ConvertMessage();

        // Send the message.
        serverSocketHandler.Send(socket, messageJson);
    }

    public void SendMessageToClient(Socket socket, EGS_Message messageToSend)
    {
        // Convert message to JSON .
        string messageJson = messageToSend.ConvertMessage();

        // Send the message.
        serverSocketHandler.Send(socket, messageJson);
    }

    /// <summary>
    /// Method Disconnect, to stop the client thread and disconnect from server.
    /// </summary>
    public void Disconnect()
    {
        // Close the socket.
        CloseClientSocket();
    }

    /// <summary>
    /// Method GetPlayersConnected, to know how many players are connected to the Game Server.
    /// </summary>
    /// <returns>Number of players connected to the Game Server</returns>
    public int GetPlayersConnected()
    {
        return serverSocketHandler.GetConnectedUsers().Count;
    }
    #endregion

    #region Private Methods
    private void CreateGame()
    {
        // Create the game data.
        EGS_GameServer.instance.thisGame = new EGS_Game(serverSocketHandler, EGS_GameServer.instance.gameFoundData.GetRoom(), "Level_0");
        // TODO: Get the scene name from the server.
    }


    /// <summary>
    /// Method CreateSocket, that creates the client socket and returns the server endpoint.
    /// </summary>
    /// <param name="serverIP">IP where server will be set</param>
    /// <param name="serverPort">Port where server will be set</param>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateSocket(string serverIP, int serverPort)
    {
        // Obtain IP address.
        IPAddress ipAddress;
        IPAddress.TryParse(serverIP, out ipAddress);

        IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

        // Create a TCP/IP socket
        socket_client = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

        // Return the EndPoint
        return remoteEP;
    }

    /// <summary>
    /// Method CreateGameServerSocket, that creates the server socket and returns the endpoint.
    /// </summary>
    /// /// <param name="gameServerPort">Port where the game server will be set</param>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateGameServerSocket(int gameServerPort)
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

    /// <summary>
    /// Method StopListening, to close the server socket and stop listening to connections.
    /// </summary>
    public void StopListening()
    {
        socket_server.Close();
    }
    #endregion
    #endregion
}
