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
    // Client socket.
    private Socket socket_client;

    // Instance of the handler for the client socket.
    public EGS_CL_SocketClient clientSocketHandler;

    // Player position.
    public static Vector3 playerPosition = new Vector3();
    #endregion

    #region Constructors
    /// <summary>
    /// Main constructor.
    /// </summary>
    public EGS_CL_Sockets()
    {

    }
    #endregion

    /// <summary>
    /// Method ConnectToServer, to establish a connection to the server.
    /// </summary>
    public void ConnectToServer()
    {
        // Create socket and get EndPoint
        EndPoint remoteEP = CreateSocket(EGS_Client.serverData.serverIP, EGS_Client.serverData.serverPort);

        // Connect to server
        clientSocketHandler = new EGS_CL_SocketClient();
        new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client)).Start();
        //clientSocketHandler.StartClient(remoteEP, socket_client);
    }

    public void SendMessage(string type, string msg)
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

    /// <summary>
    /// Method Disconnect, to stop the client thread and disconnect from server.
    /// </summary>
    public void Disconnect()
    {
        CloseSocket();
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
        // It is IPv4, but if wifi is using, it should be 1 and not 0.
        IPAddress ipAddress = ipHostInfo.AddressList[0];
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
