using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// Class EGS_SE_Sockets, that will control server sockets in the future.
/// </summary>
public class EGS_SE_Sockets
{
    #region Variables
    // Server socket.
    private Socket socket_listener;

    /// Server data
    // Server IP.
    private string serverIP;
    // Server Port.
    private int serverPort;

    // Handler for the socket listener.
    private EGS_SE_SocketController serverSocketHandler;

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
    /// Method StartListening, to start listening on the server.
    /// </summary>
    public void StartListening()
    {
        // Create socket and get EndPoint
        EndPoint localEP = CreateSocket(serverIP, serverPort);

        // Connect to server
        serverSocketHandler = new EGS_SE_SocketController(egs_Log, AfterClientConnected, OnClientDisconnected);
        serverSocketHandler.StartListening(serverPort, localEP, socket_listener);
    }

    #region Connect and disconnect methods
    /// <summary>
    /// Method AfterClientConnected, that manages a new connection
    /// </summary>
    /// <param name="clientSocket">Socket connected to the client</param>
    public void AfterClientConnected(Socket clientSocket)
    {
        if (EGS_ServerManager.DEBUG_MODE > 0)
            egs_Log.Log("<color=blue>Client</color> connected. IP: " + clientSocket.RemoteEndPoint);

        // Ask client for user data.
        EGS_Message msg = new EGS_Message();
        msg.messageType = "CONNECT";
        string jsonMSG = msg.ConvertMessage();

        serverSocketHandler.Send(clientSocket, jsonMSG);
    }

    /// <summary>
    /// Method OnClientDisconnected, that manages a disconnection
    /// </summary>
    /// <param name="clientSocket">Client socket disconnected from the server</param>
    public void OnClientDisconnected(Socket clientSocket)
    {
        if (EGS_ServerManager.DEBUG_MODE > 0)
            egs_Log.Log("<color=blue>Client</color> disconnected. IP: " + clientSocket.RemoteEndPoint);

        clientSocket.Shutdown(SocketShutdown.Both);
        clientSocket.Close();
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
    private EndPoint CreateSocket(string serverIP, int serverPort)
    {
        // Obtain IP direction and endpoint.
        IPHostEntry ipHostInfo = Dns.GetHostEntry(serverIP);
        // It is IPv4, but if wifi is using, it should be 1 and not 0.
        IPAddress ipAddress = ipHostInfo.AddressList[0];
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
        egs_Log.Log("<color=green>Easy Game Server</color> stopped listening at port <color=orange>" + serverPort + "</color>.");
    }
    #endregion
}