using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class MasterServerSocketManager, that control the server socket.
/// </summary>
public class MasterServerSocketManager
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Server socket")]
    private Socket socket_listener;

    [Tooltip("Handler for the server socket")]
    private MasterServerServerSocketHandler serverSocketHandler;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public MasterServerSocketManager()
    {

    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, to start listening on the server.
    /// </summary>
    public void StartListening()
    {
        // Create socket and get EndPoint.
        EndPoint localEP = CreateSocket();

        // Connect to server.
        serverSocketHandler = new MasterServerServerSocketHandler();
        serverSocketHandler.StartListening(localEP, socket_listener, EasyGameServerConfig.MAX_CONNECTIONS);

        Log.instance.WriteLog("<color=green>Easy Game Server</color> Listening at port <color=orange>" + EasyGameServerConfig.SERVER_PORT + "</color>.", EasyGameServerControl.EnumLogDebugLevel.Minimal);
    }

    /// <summary>
    /// Method CreateSocket, that creates the client socket and returns the server endpoint
    /// </summary>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateSocket()
    {
        IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
        IPAddress ipAddress = ips[1]; // IPv4

        // Obtain IP direction and endpoint.
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, EasyGameServerConfig.SERVER_PORT);

        // Create a TCP/IP socket.
        socket_listener = new Socket(AddressFamily.InterNetwork,
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
        Log.instance.WriteLog("<color=green>Easy Game Server</color> stopped listening connections.", EasyGameServerControl.EnumLogDebugLevel.Minimal);
    }
    #endregion
}