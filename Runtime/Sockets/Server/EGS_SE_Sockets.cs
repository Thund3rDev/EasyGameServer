using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class EGS_SE_Sockets, that control the server sockets.
/// </summary>
public class EGS_SE_Sockets
{
    #region Variables
    [Header("Server data")]
    [Tooltip("Server IP")]
    private string serverIP; // TODO: Check if neccesary.

    [Tooltip("Server Port")]
    private int serverPort;


    [Header("Networking")]
    [Tooltip("Server socket")]
    private Socket socket_listener;

    [Tooltip("Handler for the server socket")]
    private EGS_SE_SocketServer serverSocketHandler;


    [Header("References")]
    [Tooltip("Reference to the Log")]
    private EGS_Log egs_Log = null;
    #endregion

    #region Constructors
    /// <summary>
    /// Main constructor that assigns the log.
    /// </summary>
    /// <param name="log_">Log instance</param>
    /// <param name="serverIP_">IP where server will be set</param>
    /// <param name="serverPort_">Port where server will be set</param>
    public EGS_SE_Sockets(EGS_Log log_, string serverIP_, int serverPort_)
    {
        egs_Log = log_;
        serverIP = serverIP_;
        serverPort = serverPort_;
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, to start listening on the server.
    /// </summary>
    public void StartListening()
    {
        // Create socket and get EndPoint.
        //EndPoint localEP = CreateSocket(serverIP, serverPort);
        EndPoint localEP = CreateSocket();

        // Connect to server.
        serverSocketHandler = new EGS_SE_SocketServer(egs_Log, OnNewConnection, OnClientDisconnected);
        serverSocketHandler.StartListening(serverPort, localEP, socket_listener);
    }

    #region Connect and disconnect methods
    /// <summary>
    /// Method OnNewConnection, that manages a new connection.
    /// </summary>
    /// <param name="clientSocket">Socket connected to the client</param>
    public void OnNewConnection(Socket clientSocket)
    {
        if (EGS_ServerManager.DEBUG_MODE > 2)
            egs_Log.Log("<color=blue>New connection</color>. IP: " + clientSocket.RemoteEndPoint + ".");

        // Ask client for user data.
        EGS_Message msg = new EGS_Message();
        msg.messageType = "CONNECT_TO_MASTER_SERVER";
        string jsonMSG = msg.ConvertMessage();

        serverSocketHandler.Send(clientSocket, jsonMSG);
    }

    /// <summary>
    /// Method OnClientDisconnected, that manages a disconnection.
    /// </summary>
    /// <param name="clientSocket">Client socket disconnected from the server</param>
    public void OnClientDisconnected(Socket clientSocket)
    {
        if (EGS_ServerManager.DEBUG_MODE > 2)
            egs_Log.Log("<color=blue>Closed connection</color>. IP: " + clientSocket.RemoteEndPoint + ".");
    }
    #endregion
    #endregion

    #region Private Methods
    /// <summary>
    /// Method CreateSocket, that creates the client socket and returns the server endpoint
    /// </summary>
    /// <param name="serverIP">IP where server will be set</param>
    /// <param name="serverPort">Port where server will be set</param>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateSocket()
    {
        IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
        IPAddress ipAddress = ips[1]; // IPv4

        // Obtain IP direction and endpoint.
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, serverPort);

        // Create a TCP/IP socket.
        socket_listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Return the EndPoint
        return localEndPoint;
    }

    /// <summary>
    /// Method StopListening, to close the socket and stop listening to connections.
    /// </summary>
    public void StopListening()
    {
        socket_listener.Close();
        egs_Log.Log("<color=green>Easy Game Server</color> stopped listening connections.");
    }
    #endregion
}