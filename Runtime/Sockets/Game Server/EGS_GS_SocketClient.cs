using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_GS_SocketClient, that controls the client socket for the game server.
/// </summary>
public class EGS_GS_SocketClient
{
    #region Variables
    [Header("ManualResetEvents")]
    [Tooltip("ManualResetEvent for when connection is done")]
    private ManualResetEvent connectDone = new ManualResetEvent(false); // TODO: Valorate if needed.
    [Tooltip("ManualResetEvent for when send is done.")]
    private ManualResetEvent sendDone = new ManualResetEvent(false); // TODO: Valorate if needed.
    [Tooltip("ManualResetEvent for when receive is done")]
    private ManualResetEvent receiveDone = new ManualResetEvent(false); // TODO: Valorate if needed.

    /// Other
    // Response from the remote device.
    private string response = string.Empty; // TODO: Check if this should be here: overwrited data and too large data.

    [Header("Networking")]
    [Tooltip("Socket for the client")]
    private Socket socket_client; // TODO: Pass it to EGS_CL_Sockets.
    [Tooltip("Sockets Controller")]
    private EGS_GS_Sockets socketsController; // TODO: Valorate if needed.

    [Tooltip("Thread for the KeepAlive function")]
    public Thread keepAliveThread;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public EGS_GS_SocketClient(EGS_GS_Sockets socketsController_)
    {
        socketsController = socketsController_;
    }
    #endregion

    #region Class Methods
    #region Public Methods
    /// <summary>
    /// Method StartClient, that tries to connect to the server.
    /// </summary>
    /// <param name="remoteEP">EndPoint where the server is</param>
    /// <param name="socket_client">Socket to use</param>
    public void StartClient(EndPoint remoteEP, Socket socket_client_)
    {
        // Assign the socket
        socket_client = socket_client_;

        // Reset the ManualResetEvents.
        connectDone.Reset();
        sendDone.Reset();
        receiveDone.Reset();

        // Connect to a remote device.
        try
        {
            // Connect to the remote endpoint.  
            socket_client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), socket_client);

            // Wait until the connection is done.
            connectDone.WaitOne();

            // Receive the response from the remote device.  
            Receive(socket_client);
            receiveDone.WaitOne();
        }
        catch (ThreadAbortException)
        {
            // TODO: Control this exception.
        }
        catch (Exception e)
        {
           Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method ConnectCallback, called when connected to server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            Debug.Log("[CLIENT] Socket connected to " +
                client.RemoteEndPoint.ToString());

            // TODO: Check if this should be here.
            EGS_GameServer.gameServer_instance.connectedToMasterServer = true;

            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method Receive, to receive data from server.
    /// </summary>
    /// <param name="client">Socket</param>
    public void Receive(Socket client)
    {
        try
        {
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = client;

            // Begin receiving the data from the remote device.  
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method ReceiveCallback, called when received data from server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.  
            int bytesRead = 0;

            // TODO: Don't use true.
            if (EGS_GameServer.gameServer_instance.connectedToMasterServer)
            {
                try
                {
                    bytesRead = client.EndReceive(ar);
                }
                catch (ObjectDisposedException)
                {
                    // TODO: Control this exception.
                }
            }

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                if (state.sb.ToString().EndsWith("<EOM>"))
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 0)
                    {
                        response = state.sb.ToString();
                    }

                    // Signal that all bytes have been received.  
                    receiveDone.Set();

                    // Keep receiving messages from the server.
                    Receive(socket_client);

                    // Split if there is more than one message
                    string[] receivedMessages = response.Split(new string[] { "<EOM>" }, StringSplitOptions.None);

                    // Handle the messages (split should leave one empty message at the end so we skip it by substract - 1 to the length)
                    for (int i = 0; i < (receivedMessages.Length - 1); i++)
                        HandleMessage(receivedMessages[i], socket_client);
                }
                else
                {
                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
            }
        }
        catch (ThreadAbortException)
        {
            // TODO: Control this exception.
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method Send, for send a message to the server.
    /// </summary>
    /// <param name="client">Client Socket</param>
    /// <param name="data">String with the data to send</param>
    public void Send(Socket client, string data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method SendCallback, called when sent data to server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            if (EGS_Config.DEBUG_MODE > 2)
                Debug.Log("[CLIENT] Sent " + bytesSent + " bytes to server.");

            // Signal that all bytes have been sent.  
            sendDone.Set();
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method HandleMessage, that receives a message from the server and do things based on it.
    /// </summary>
    /// <param name="content">Message content</param>
    /// <param name="handler">Socket that handles that connection</param>
    private void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        EGS_Message receivedMessage = new EGS_Message();
        try
        {
            receivedMessage = JsonUtility.FromJson<EGS_Message>(content);
        }
        catch (Exception e)
        {
            Debug.LogWarning("ERORR, CONTENT: " + content);
            throw e;
        }

        if (EGS_Config.DEBUG_MODE > 2)
            Debug.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);

        // Message to send back.
        EGS_Message messageToSend = new EGS_Message();

        // Local variables that are used in the cases below.
        string jsonMSG;

        // Depending on the messageType, do different things
        switch (receivedMessage.messageType)
        {
            case "TEST_MESSAGE":
                Debug.Log("Received test message from server: " + receivedMessage.messageContent);
                break;
            case "RTT":
                // TODO: Save the time elapsed between RTTs.
                messageToSend.messageType = "RTT_RESPONSE_GAME_SERVER";
                messageToSend.messageContent = EGS_GameServer.gameServer_instance.gameServerID.ToString();

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(socket_client, jsonMSG);

                // Wait until send is done.
                sendDone.WaitOne();
                break;
            case "CONNECT_TO_MASTER_SERVER":
                // Change the server state.
                EGS_GameServer.gameServer_instance.gameServerState = EGS_GameServerData.EGS_GameServerState.CREATED;
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text = "Status: " + Enum.GetName(typeof(EGS_GameServerData.EGS_GameServerState), EGS_GameServer.gameServer_instance.gameServerState); });

                // Start listening for player connections and wait until it is started.
                socketsController.StartListening();
                socketsController.startDone.WaitOne();

                // Send a message to the master server.
                messageToSend.messageType = "CREATED_GAME_SERVER";

                string gameServerIP = EGS_Config.serverIP + ":" + EGS_GameServer.gameServer_instance.gameServerPort;
                messageToSend.messageContent = EGS_GameServer.gameServer_instance.gameServerID + "#" + gameServerIP;
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nIPADRESS " + EGS_Config.serverIP; });

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(socket_client, jsonMSG);

                // Wait until send is done.
                sendDone.WaitOne();
                break;
            case "CLOSE_GAME_SERVER":
                EGS_GameServer.gameServer_instance.connectedToMasterServer = false;
                break;
            default:
                Debug.Log("<color=yellow>Undefined message type: </color>" + receivedMessage.messageType);
                break;
        }
    }
    #endregion
    #endregion
}