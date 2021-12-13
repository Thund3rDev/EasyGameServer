using System;
using System.Collections.Generic;
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

    // TODO: This CAN'T BE HERE.
    // Player positions and usernames.
    public static Dictionary<int, Vector3> playerPositions = new Dictionary<int, Vector3>();
    public static Dictionary<int, string> playerUsernames = new Dictionary<int, string>();

    // TODO: This CAN'T BE HERE.
    // Test:
    public EGS_User thisUser;
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
        // Obtain IP Address.
        IPAddress serverIpAddress = ObtainIPAddress(EGS_Client.serverData.serverIP);
        // Create the Client Socket.
        CreateClientSocket(serverIpAddress);
        // Get EndPoint.
        EndPoint remoteEP = CreateEndpoint(serverIpAddress, EGS_Client.serverData.serverPort);

        // Connect to server.
        clientSocketHandler = new EGS_CL_SocketClient(this);
        clientSocketHandler.StartClient(remoteEP, socket_client);

        // TODO: Value if Thread is necessary or not.
        //new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client)).Start();
    }

    /// <summary>
    /// Method ConnectToGameServer, to establish a connection to the game server.
    /// </summary>
    public void ConnectToGameServer(string serverIP, int serverPort)
    {
        // Obtain IP Address.
        IPAddress gameServerIpAddress = ObtainIPAddress(serverIP);

        // Create the Client Socket. TODO: PROBLEMA AQUI
        CreateClientSocket(gameServerIpAddress);

        // Get EndPoint.
        EndPoint remoteEP = CreateEndpoint(gameServerIpAddress, serverPort);

        EGS_Client.connectedToGameServer = true;

        // Connect to game server.
        clientSocketHandler.StartClient(remoteEP, socket_client);

        // TODO: Value if Thread is necessary or not.
        //new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client)).Start();
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
    /// Method DisconnectFromServer, to stop the client thread and disconnect from server.
    /// </summary>
    public void DisconnectFromServer()
    {
        SendMessage("DISCONNECT_USER", "");
    }

    /// <summary>
    /// Method CloseSocket, to close the client socket.
    /// </summary>
    public void CloseSocket()
    {
        socket_client.Shutdown(SocketShutdown.Both);
        socket_client.Close();

        Debug.Log("[CLIENT] Closed socket.");
    }

    #region Private Methods
    #region Network data
    /// <summary>
    /// Method CreateClientSocket, that creates the client socket.
    /// </summary>
    /// <param name="ipAddress">IPAddress to which the socket will connect</param>
    private void CreateClientSocket(IPAddress ipAddress)
    {
        // Create a TCP/IP socket.
        socket_client = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);
    }

    /// <summary>
    /// Method CreateEndpoint, that creates and returns the endpoint to the server.
    /// </summary>
    /// <param name="ipAddress">IPAddress for that Endpoint</param>
    /// <param name="serverPort">Port where the server is</param>
    /// <returns></returns>
    private EndPoint CreateEndpoint(IPAddress ipAddress, int serverPort)
    {
        // Create the remote EndPoint.
        IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

        // Return the EndPoint.
        return remoteEP;
    }

    /// <summary>
    /// Method ObtainIPAddress, that returns the IPAddress of the given IP string.
    /// </summary>
    /// <param name="serverIP">String containing the server IP</param>
    /// <returns></returns>
    private IPAddress ObtainIPAddress(string serverIP)
    {
        // Obtain IP address.
        IPAddress ipAddress;
        IPAddress.TryParse(serverIP, out ipAddress);

        // Return thee IPAddress.
        return ipAddress;
    }
    #endregion
    #endregion
}
