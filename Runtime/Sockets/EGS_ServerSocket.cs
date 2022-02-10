using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using System.Timers;

/// <summary>
/// Class EGS_ServerSocket, that controls a server listener socket.
/// </summary>
public class EGS_ServerSocket
{
    #region Variables
    // TODO: Move Delegates to a static class, singleton or global object.
    [Header("Delegates")]
    [Tooltip("Delegate to the OnNewConnection function")]
    protected Action<Socket> onNewConnection;
    [Tooltip("Delegate to the OnClientDisconnect function")]
    protected Action<Socket> onDisconnectDelegate;


    [Header("User data")]
    [Tooltip("Dictionary that stores ALL users by their username")] // TODO: Make this By ID.
    protected Dictionary<string, EGS_User> allUsers = new Dictionary<string, EGS_User>();
    [Tooltip("Dictionary that stores the CURRENTLY CONNECTED users by their socket")]
    protected Dictionary<Socket, EGS_User> connectedUsers = new Dictionary<Socket, EGS_User>();

    [Tooltip("Dictionary that stores the timer by socket to check if still connected")]
    protected Dictionary<Socket, EGS_RoundTripTime> roundTripTimes = new Dictionary<Socket, EGS_RoundTripTime>();
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public EGS_ServerSocket(Action<Socket> onNewConnection, Action<Socket> onClientDisconnect)
    {
        this.onNewConnection = onNewConnection;
        this.onDisconnectDelegate = onClientDisconnect;
    }
    #endregion

    #region Class Methods
    #region Public Methods
    /// <summary>
    /// Method StartListening, that opens the socket to connections.
    /// </summary>
    /// <param name="remoteEP">EndPoint where the server is</param>
    /// <param name="socket_listener">Socket to use</param>
    public virtual void StartListening(EndPoint localEP, Socket socket_listener, int connections)
    {
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            socket_listener.Bind(localEP);
            socket_listener.Listen(connections);

            // Start listening for connections asynchronously.
            socket_listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                socket_listener);
        }
        catch (ThreadAbortException)
        {
            //egs_Log.LogWarning("Aborted server thread"); // TODO: Control this Exception.
        }
        catch (Exception)
        {
            //egs_Log.LogWarning("Aborted server thread"); // TODO: Control this Exception.
        }
    }

    /// <summary>
    /// Method AcceptCallback, called when a client connects to the server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public void AcceptCallback(IAsyncResult ar)
    {
        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Do things on client connected.
        onNewConnection(handler);

        // Create the state object and begin receive.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);

        // Start listening for connections asynchronously.
        listener.BeginAccept(
            new AsyncCallback(AcceptCallback),
            listener);
    }

    /// <summary>
    /// Method ReadCallback, called when a client sends a message.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    protected virtual void ReadCallback(IAsyncResult ar)
    {
        // Retrieve the state object and the handler socket from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Message content from the remote device.
        string content = string.Empty;

        // Read data from the client socket.
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            if (state.sb.ToString().EndsWith("<EOM>"))
            {
                // All the data has arrived; put it in response.  
                if (state.sb.Length > 0)
                {
                    content = state.sb.ToString();
                }

                // Keep receiving for that socket.
                state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);

                // Split if there is more than one message
                string[] receivedMessages = content.Split(new string[] { "<EOM>" }, StringSplitOptions.None);

                // Handle the messages (split should leave one empty message at the end so we skip it by substract - 1 to the length)
                for (int i = 0; i < (receivedMessages.Length - 1); i++)
                    HandleMessage(receivedMessages[i], handler);
            }
            else
            {
                // Get the rest of the data.  
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
        }
    }

    /// <summary>
    /// Method Send, to send a message to a client.
    /// </summary>
    /// <param name="handler">Socket</param>
    /// <param name="data">String that contains the data to send</param>
    public void Send(Socket handler, string data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method SendCallback, called when a message was sent.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    protected virtual void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);

        }
        catch (SocketException)
        {
            // TODO: Control this Exception.
        }
        catch (Exception)
        {
            // TODO: Control this Exception.
        }
    }

    /// <summary>
    /// Method HandleMessage, that receives a message from a client and do things based on it.
    /// </summary>
    /// <param name="content">Message content</param>
    /// <param name="handler">Socket that handles that connection</param>
    protected virtual void HandleMessage(string content, Socket handler)
    {

    }

    #region User Management Methods

    /// <summary>
    /// Method ConnectUser, that connects an user to the server.
    /// </summary>
    /// <param name="userToConnect">User to connect to the server</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    protected virtual void ConnectUser(EGS_User userToConnect, Socket client_socket)
    {
        // Update its socket.
        allUsers[userToConnect.GetUsername()].SetSocket(client_socket);

        // Set its user ID.
        userToConnect.SetUserID(allUsers[userToConnect.GetUsername()].GetUserID());

        // Save user data on the dictionary of connectedUsers.
        lock (connectedUsers)
        {
            connectedUsers.Add(client_socket, userToConnect);
        }
    }

    /// <summary>
    /// Method DisconnectUser, that disconnects an user from the server.
    /// </summary>
    /// <param name="userToDisconnect">User to disconnect from the server</param>
    protected virtual void DisconnectUser(EGS_User userToDisconnect)
    {
        // Get the socket.
        userToDisconnect.SetSocket(allUsers[userToDisconnect.GetUsername()].GetSocket());

        // Disconnect it from server.
        lock (connectedUsers)
        {
            connectedUsers.Remove(userToDisconnect.GetSocket());
        }

        // Disconnect the client.
        DisconnectClient(userToDisconnect.GetSocket());
    }
    #endregion

    #region Networking Methods
    /// <summary>
    /// Method DisconnectClient, that disconnect a client from the server.
    /// </summary>
    /// <param name="client_socket">Socket that handles the client</param>
    public void DisconnectClient(Socket client_socket)
    {
        onDisconnectDelegate(client_socket);
        StopRTT(client_socket);
    }

    /// <summary>
    /// Method CreateRTT, that puts a round trip time to check if clients are still connected.
    /// </summary>
    /// <param name="client_socket">Socket that handles the client</param>
    protected virtual void CreateRTT(Socket client_socket)
    {
        EGS_RoundTripTime thisRoundTripTime = new EGS_RoundTripTime(this, client_socket);
        roundTripTimes.Add(client_socket, thisRoundTripTime);
    }

    /// <summary>
    /// Method StopRTT, that stops the timer assigned to the client socket.
    /// </summary>
    /// <param name="client_socket">Socket that handles the client</param>
    protected virtual void StopRTT(Socket client_socket)
    {
        lock (roundTripTimes)
        {
            roundTripTimes[client_socket].StopRTT();
            roundTripTimes.Remove(client_socket);
        }
    }

    /// <summary>
    /// Method DisconnectClientByTimeout, to disconnect a client when the timer was completed.
    /// </summary>
    /// <param name="sender">Object needed by the timer</param>
    /// <param name="e">ElapsedEventArgs needed by the timer</param>
    /// <param name="client_socket">Socket that handles the client</param>
    public virtual void DisconnectClientByTimeout(object sender, ElapsedEventArgs e, Socket client_socket)
    {
        // Disconnect the client socket.
        EGS_User userToDisconnect = connectedUsers[client_socket];
        DisconnectClient(client_socket);

        // Remove the user from the connectedUsers dictionary.
        connectedUsers.Remove(client_socket);
    }
    #endregion

    /// <summary>
    /// Method TestMessage, for testing purposes.
    /// </summary>
    /// <param name="client_socket">Socket that handles the client</param>
    private void TestMessage(Socket client_socket)
    {
        // Send the test message
        EGS_Message msg = new EGS_Message();
        msg.messageType = "TEST_MESSAGE";
        string jsonMSG = msg.ConvertMessage();

        Send(client_socket, jsonMSG);
    }
    #endregion
    #endregion
}