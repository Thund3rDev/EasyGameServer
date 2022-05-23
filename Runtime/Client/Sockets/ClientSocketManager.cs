using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class ClientSocketManager, that controls client sockets.
/// </summary>
public class ClientSocketManager
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Client socket")]
    private Socket socket_client;

    [Tooltip("Handler for the client socket")]
    private ClientClientSocketHandler clientSocketHandler;

    [Tooltip("Thread that handles the connections")]
    private Thread connectionsThread;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public ClientSocketManager()
    {

    }
    #endregion

    #region Class Methods
    #region Connections
    /// <summary>
    /// Method ConnectToServer, to establish a connection to the server.
    /// </summary>
    public void ConnectToServer()
    {
        // Obtain IP Address.
        IPAddress serverIpAddress = ObtainIPAddress(EasyGameServerConfig.SERVER_IP);

        // Create the Client Socket.
        CreateClientSocket();

        // Get EndPoint.
        EndPoint remoteEP = CreateEndpoint(serverIpAddress, EasyGameServerConfig.SERVER_PORT);

        // Connect to server.
        clientSocketHandler = new ClientClientSocketHandler(this);
        connectionsThread = new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client));
        connectionsThread.Start();
    }

    /// <summary>
    /// Method ConnectToGameServer, to establish a connection to the game server.
    /// </summary>
    public void ConnectToGameServer(string serverIP, int serverPort)
    {
        // Obtain IP Address.
        IPAddress gameServerIpAddress = ObtainIPAddress(serverIP);

        // Create the Client Socket. TODO: PROBLEMA AQUI: HAY QUE PARAR EL THREAD ANTERIOR Y LANZAR EL NUEVO!
        CreateClientSocket();

        // Get EndPoint.
        EndPoint remoteEP = CreateEndpoint(gameServerIpAddress, serverPort);

        // Connect to game server.
        clientSocketHandler.StartClient(remoteEP, socket_client);

        // TODO: SI. Tener en cuenta el thread anterior.
        // connectionsThread = new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client));
        // connectionsThread.Start();
    }

    /// <summary>
    /// Method DisconnectFromServer, to stop the client thread and disconnect from server.
    /// </summary>
    public void DisconnectFromServer()
    {
        SendMessage("DISCONNECT_USER", "");
    }
    #endregion

    #region Messaging
    /// <summary>
    /// Method SendMessage, to send a message to the server given its type and content.
    /// </summary>
    /// <param name="messageType">Message type</param>
    /// <param name="messageContent">Message content</param>
    public void SendMessage(string messageType, string messageContent)
    {
        // Create new message.
        NetworkMessage thisMessage = new NetworkMessage(messageType, messageContent);

        // Convert message to JSON.
        string messageJson = thisMessage.ConvertMessage();

        // Send the message.
        clientSocketHandler.Send(socket_client, messageJson);
    }

    /// <summary>
    /// Method SendMessage, to send a message to the server.
    /// </summary>
    /// <param name="messageToSend">NetworkMessage to send</param>
    public void SendMessage(NetworkMessage messageToSend)
    {
        // Convert message to JSON.
        string messageJson = messageToSend.ConvertMessage();

        // Send the message.
        clientSocketHandler.Send(socket_client, messageJson);
    }
    #endregion

    #region Sockets
    /// <summary>
    /// Method CloseSocket, to close the client socket.
    /// </summary>
    public void CloseSocket()
    {
        socket_client.Shutdown(SocketShutdown.Both);
        socket_client.Close();

        Debug.Log("[CLIENT] Closed socket.");
    }

    /// <summary>
    /// Method CloseSocketOnApplicationQuit, to close the client socket when closing the application.
    /// </summary>
    public void CloseSocketOnApplicationQuit()
    {
        try
        {
            connectionsThread.Interrupt();

            if (socket_client.Connected)
                CloseSocket();
        }
        catch (SocketException se)
        {
            Debug.LogError("[CLIENT] SocketException: " + se.ToString());
        }
        catch (ObjectDisposedException ode)
        {
            Debug.LogError("[CLIENT] ObjectDisposedException: " + ode.ToString());
        }
    }
    #endregion

    #region Network data
    /// <summary>
    /// Method CreateClientSocket, that creates the client socket.
    /// </summary>
    private void CreateClientSocket()
    {
        // Create a TCP/IP socket.
        socket_client = new Socket(AddressFamily.InterNetwork,
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

        // Return the IPAddress.
        return ipAddress;
    }
    #endregion
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the ConnectionsThread.
    /// </summary>
    /// <returns>Connections Thread</returns>
    public Thread GetConnectionsThread() { return connectionsThread; }

    /// <summary>
    /// Setter for the ConnectionsThread.
    /// </summary>
    /// <param name="connectionsThread">New Connections Thread</param>
    public void SetConnectionsThread(Thread connectionsThread) { this.connectionsThread = connectionsThread; }
    #endregion
}
