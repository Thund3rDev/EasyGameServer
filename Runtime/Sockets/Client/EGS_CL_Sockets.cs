using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_CL_Sockets, that controls client sockets.
/// </summary>
public class EGS_CL_Sockets
{
    #region Variables
    // Thread that runs the client (just for testing purposes, probably unnecesary).
    private Thread clientThread;

    // Client socket.
    private Socket socket_client;

    // Boolean that indicates if client is connected to the server.
    public static bool connectedToServer;

    /// References
    // Reference to the Log.
    private EGS_Log egs_Log = null;
    #endregion

    #region Constructors
    /// <summary>
    /// Main constructor that assigns the log.
    /// </summary>
    /// <param name="log_">Log instance</param>
    public EGS_CL_Sockets(EGS_Log log_)
    {
        egs_Log = log_;
        connectedToServer = false;
    }
    #endregion

    /// <summary>
    /// Method ConnectToServer, to establish a connection to the server.
    /// </summary>
    /// <param name="serverIP">IP where server will be set</param>
    /// <param name="serverPort">Port where server will be set</param>
    public void ConnectToServer(string serverIP, int serverPort)
    {
        // Create socket and get EndPoint
        EndPoint remoteEP = CreateSocket(serverIP, serverPort);

        // Connect to server
        EGS_CL_SocketClient clientSocketHandler = new EGS_CL_SocketClient();
        clientThread = new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client));
        clientThread.Start();
    }

    /// <summary>
    /// Method Disconnect, to stop the client thread and disconnect from server.
    /// </summary>
    public void Disconnect()
    {
        clientThread.Abort();
        clientThread.Join();
        CloseSocket();
        connectedToServer = false;
    }

    #region Private Methods
    /// <summary>
    /// Method CreateSocket, that creates the client socket and returns the server endpoint
    /// </summary>
    /// <param name="serverIP">IP where server will be set</param>
    /// <param name="serverPort">Port where server will be set</param>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateSocket(string serverIP, int serverPort)
    {
        // Obtain IP direction and endpoint
        IPHostEntry ipHostInfo = Dns.GetHostEntry(serverIP);
        // It is IPv4, for IPv6 it would be 0.
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

        // Create a TCP/IP socket
        socket_client = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Return the EndPoint
        return remoteEP;
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
}
