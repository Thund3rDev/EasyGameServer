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
    public EGS_GS_SocketClient clientSocketHandler;

    [Tooltip("Server Socket")]
    private Socket socket_server;

    [Tooltip("Handler for the server socket")]
    private EGS_GS_SocketServer serverSocketHandler;

    [Tooltip("EndPoint to the game server")]
    public EndPoint localEP;

    [Tooltip("ManualResetEvent for when game server can handle player connections")]
    public ManualResetEvent startDone = new ManualResetEvent(false);
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
        // Create socket and get EndPoint
        EndPoint remoteEP = CreateSocket(EGS_Config.serverIP, EGS_Config.serverPort);

        // Connect to server
        clientSocketHandler = new EGS_GS_SocketClient(this);
        new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client)).Start();
    }

    /// <summary>
    /// Method StartListening, to start listeting connections from clients.
    /// </summary>
    public void StartListening()
    {
        // Create socket and get EndPoint.
        localEP = CreateGameServerSocket(EGS_GameServer.gameServer_instance.gameServerPort);

        // Connect to server.
        serverSocketHandler = new EGS_GS_SocketServer(this, AfterPlayerConnected, OnPlayerDisconnected);
        new Thread(() => serverSocketHandler.StartListening(localEP, socket_server)).Start();

        // Create the Game.
        CreateGame();
    }

    public void SendMessageToMasterServer(string type, string msg)
    {
        // Create new message.
        EGS_Message thisMessage = new EGS_Message();
        thisMessage.messageType = type;
        thisMessage.messageContent = msg;

        // Convert message to JSON .
        string messageJson = thisMessage.ConvertMessage();

        // Send the message.
        clientSocketHandler.Send(socket_client, messageJson);
    }

    #region Connect and disconnect methods
    /// <summary>
    /// Method AfterPlayerConnected, that manages a new connection.
    /// </summary>
    /// <param name="clientSocket">Socket connected to the client</param>
    public void AfterPlayerConnected(Socket clientSocket)
    {
        // Ask client for user data.
        EGS_Message msg = new EGS_Message();
        msg.messageType = "CONNECT_GAME_SERVER";
        msg.messageContent = EGS_GameServer.gameServer_instance.thisGame.GetRoom().ToString();
        string jsonMSG = msg.ConvertMessage();

        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nPLAYER CONNECTED: " + clientSocket.RemoteEndPoint; });
        serverSocketHandler.Send(clientSocket, jsonMSG);
    }

    /// <summary>
    /// Method OnPlayerDisconnected, that manages a disconnection.
    /// </summary>
    /// <param name="clientSocket">Client socket disconnected from the server</param>
    public void OnPlayerDisconnected(Socket clientSocket)
    {
        // TODO: Make this work.
    }
    #endregion

    /// <summary>
    /// Method Disconnect, to stop the client thread and disconnect from server.
    /// </summary>
    public void Disconnect()
    {
        // Close the socket.
        CloseSocket();
    }
    #endregion

    #region Private Methods
    private void CreateGame()
    {
        // Create the game data.
        EGS_GameServer.gameServer_instance.thisGame = new EGS_Game(serverSocketHandler, EGS_GameServer.gameServer_instance.startData.GetRoom());
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
        socket_client = new Socket(ipAddress.AddressFamily,
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
        socket_server = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Return the EndPoint
        return localEndPoint;
    }

    /// <summary>
    /// Method CloseSocket, to close the client socket.
    /// </summary>
    private void CloseSocket()
    {
        socket_client.Shutdown(SocketShutdown.Both);
        socket_client.Close();

        Debug.Log("[CLIENT] Closed socket.");
    }
    #endregion
    #endregion
}
