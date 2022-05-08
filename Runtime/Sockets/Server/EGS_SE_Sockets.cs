using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class EGS_SE_Sockets, that control the server sockets.
/// </summary>
public class EGS_SE_Sockets
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Server socket")]
    private Socket socket_listener;

    [Tooltip("Handler for the server socket")]
    private EGS_SE_ServerSocket serverSocketHandler;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_SE_Sockets() {}
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
        serverSocketHandler = new EGS_SE_ServerSocket();
        serverSocketHandler.StartListening(localEP, socket_listener, EGS_Config.MAX_CONNECTIONS);

        EGS_Log.instance.Log("<color=green>Easy Game Server</color> Listening at port <color=orange>" + EGS_Config.serverPort + "</color>.", EGS_Control.EGS_DebugLevel.Minimal);
    }
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
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, EGS_Config.serverPort);

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
        EGS_Log.instance.Log("<color=green>Easy Game Server</color> stopped listening connections.", EGS_Control.EGS_DebugLevel.Minimal);
    }
    #endregion
}