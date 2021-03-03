using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_SE_SocketListener, that controls the server receiver socket.
/// </summary>
public class EGS_SE_SocketListener
{
    #region Variables
    /// ManualResetEvents
    // ManualResetEvent allDone for connection.
    public ManualResetEvent allDone = new ManualResetEvent(false);

    /// References
    // Reference to the Log.
    private EGS_Log egs_Log = null;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_SE_SocketListener(EGS_Log log)
    {
        egs_Log = log;
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, that opens the socket to connections.
    /// </summary>
    /// <param name="serverPort">Port where the server is</param>
    /// <param name="remoteEP">EndPoint where the server is</param>
    /// <param name="socket_client">Socket to use</param>
    public void StartListening(int serverPort, EndPoint localEP, Socket socket_listener)
    {
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            socket_listener.Bind(localEP);
            socket_listener.Listen(100);

            egs_Log.Log("<color=green>Easy Game Server</color> Listening at port <color=orange>" + serverPort + "</color>.");

            while (true)
            {
                allDone.Reset();
                // Start an asynchronous socket to listen for connections.
                socket_listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    socket_listener);
                allDone.WaitOne();
            }
        }
        catch(ThreadAbortException)
        {
            //egs_Log.LogWarning("Aborted server thread");
        }
        catch (Exception e)
        {  
            egs_Log.LogError(e.ToString());
        } 

    }

    /// <summary>
    /// Method AcceptCallback, called when a client connects to the server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public void AcceptCallback(IAsyncResult ar)
    {
        allDone.Set();
        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        egs_Log.Log("<color=blue>Client</color> connected. IP: " + handler.RemoteEndPoint);

        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    /// <summary>
    /// Method ReadCallback, called when a client sends a message.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public void ReadCallback(IAsyncResult ar)
    {
        string content = string.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Read message data.
            content = state.sb.ToString();

            // Handle the message
            HandleMessage(content, handler);
        }
    }

    /// <summary>
    /// Method Send, to send a message to a client.
    /// </summary>
    /// <param name="handler">Socket</param>
    /// <param name="data">String that contains the data to send</param>
    private void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    /// <summary>
    /// Method SendCallback, called when a message was sent.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            egs_Log.Log("Sent " + bytesSent + " bytes to client.");

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        }
        catch (Exception e)
        {
            egs_Log.LogError(e.ToString());
        }
    }

    private void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        EGS_Message receivedMessage = new EGS_Message();
        receivedMessage = JsonUtility.FromJson<EGS_Message>(content);

        // Depending on the messageType, do different things
        switch (receivedMessage.messageType)
        {
            case "connect":
                // Get the received user
                EGS_User receivedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);

                // Display data on the console.  
                egs_Log.Log("Read " + content.Length + " bytes from socket. \n<color=purple>Data:</color> UserID: " + receivedUser.userID + " - Username: " + receivedUser.username);

                // Echo the data back to the client.
                string messageToSend = "Welcome, " + receivedUser.username;
                Send(handler, messageToSend);
                break;
            case "test_message":
                // Display data on the console.  
                egs_Log.Log("Read " + content.Length + " bytes from socket. \n<color=purple>Data:</color>" + receivedMessage.messageContent);
                break;
            default:
                egs_Log.Log("Read " + content.Length + " bytes from socket. \n<color=yellow>Undefined message type: </color>" + receivedMessage.messageType);
                break;
        }
    }
    #endregion
}