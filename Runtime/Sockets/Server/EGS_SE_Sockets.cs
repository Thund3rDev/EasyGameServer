using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_SE_Sockets, that will control server sockets in the future.
/// </summary>
public class EGS_SE_Sockets
{
    #region Variables
    // Thread that runs the server.
    private Thread serverThread;

    // Server socket.
    private Socket socket_listener;

    // Bool that indicates if server is in debug mode.
    private static readonly bool DEBUG_MODE = true;

    /// Server data
    // Server IP.
    private string serverIP;
    // Server Port.
    private int serverPort;

    /// References
    // Reference to the Log.
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
    /// Method StartServer, to start listening on the server.
    /// </summary>
    public void StartServer()
    {
        // Create socket and get EndPoint
        EndPoint localEP = CreateSocket(serverIP, serverPort);

        // Connect to server
        EGS_SE_SocketListener serverSocketHandler = new EGS_SE_SocketListener(egs_Log, AfterClientConnected);
        serverThread = new Thread(() => serverSocketHandler.StartListening(serverPort, localEP, socket_listener));
        serverThread.Start();
    }

    /// <summary>
    /// Method AfterClientConnected, that manages a new connection
    /// </summary>
    /// <param name="clientSocket">Socket connected to the client</param>
    public void AfterClientConnected(Socket clientSocket)
    {
        // Create an empty user for the new connection.
        EGS_User user = new EGS_User();

        // Set its socket.
        user.setSocket(clientSocket);

        // Save it for later use.
        // usersConnected.add(clientSocket, user);

        // Ask client for user data.
        EGS_Message msg = new EGS_Message();
        msg.messageType = "JOIN";

        // Send(clientSocket, msg);

        if (DEBUG_MODE)
        {
            egs_Log.Log("<color=blue>Client</color> connected. IP: " + clientSocket.RemoteEndPoint);
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method CreateSocket, that creates the client socket and returns the server endpoint
    /// </summary>
    /// <param name="serverIP">IP where server will be set</param>
    /// <param name="serverPort">Port where server will be set</param>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateSocket(string serverIP, int serverPort)
    {
        // Obtain IP direction and endpoint.
        IPHostEntry ipHostInfo = Dns.GetHostEntry(serverIP);
        // It is IPv4, for IPv6 it would be 0.
        IPAddress ipAddress = ipHostInfo.AddressList[1];
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
        serverThread.Abort();
        serverThread.Join();
        socket_listener.Close();
        egs_Log.Log("<color=green>Easy Game Server</color> stopped listening at port <color=orange>" + serverPort + "</color>.");
    }
    #endregion
}