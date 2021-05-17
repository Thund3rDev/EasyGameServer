using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class EGS_CL_SocketClient, that controls the client sender socket.
/// </summary>
public class EGS_CL_SocketClient
{
    #region Variables
    /// ManualResetEvents
    // ManualResetEvent for when connection is done.
    private ManualResetEvent connectDone = new ManualResetEvent(false);
    // ManualResetEvent for when send is done.
    private ManualResetEvent sendDone = new ManualResetEvent(false);
    // ManualResetEvent for when receive is done.
    private ManualResetEvent receiveDone = new ManualResetEvent(false);

    /// Other
    // Response from the remote device.
    private string response = string.Empty;

    // Socket for the client.
    private Socket socket_client;

    // Thread for the KeepAlive function.
    public Thread keepAliveThread;

    // This client room. Test.
    int room = -1;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_CL_SocketClient()
    {
    }
    #endregion

    #region Class Methods
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

            EGS_Client.connectedToServer = true;

            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
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
    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;
            // Read data from the remote device.  
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Read message data.
                response = state.sb.ToString();
            }

            // Split if there is more than one message
            string[] receivedMessages = response.Split(new string[] { "<EOM>" }, StringSplitOptions.None);

            // Handle the messages (split should leave one empty message at the end so we skip it by substract - 1 to the length)
            for (int i = 0; i < (receivedMessages.Length - 1); i++)
                HandleMessage(receivedMessages[i], socket_client);

            // Signal that all bytes have been received.  
            receiveDone.Set();
        }
        catch (ThreadAbortException)
        {
            //egs_Log.LogWarning("Aborted server thread");
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method Send, for send a message to the server.
    /// </summary>
    /// <param name="client">Socket</param>
    /// <param name="data">String with the data to send</param>
    public void Send(Socket client, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
    }

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
            if (EGS_ServerManager.DEBUG_MODE > 1)
                Debug.Log("[CLIENT] Sent " + bytesSent + " bytes to server.");

            // Signal that all bytes have been sent.  
            sendDone.Set();
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    private void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        EGS_Message receivedMessage = new EGS_Message();
        receivedMessage = JsonUtility.FromJson<EGS_Message>(content);

        if (EGS_ServerManager.DEBUG_MODE > 1)
            Debug.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);

        // Message to send.
        EGS_Message msg;
        string jsonMSG;

        // Depending on the messageType, do different things
        switch (receivedMessage.messageType)
        {
            case "TEST_MESSAGE":
                Debug.Log("Received test message from server");
                break;
            case "CONNECT":
                // Test data.
                EGS_User thisUser = new EGS_User();
                thisUser.SetUserID(0);
                thisUser.SetUsername(EGS_Client.client_instance.username);

                // Convert user to JSON.
                string userJson = JsonUtility.ToJson(thisUser);

                msg = new EGS_Message();
                msg.messageType = "JOIN_SERVER";
                msg.messageContent = userJson;

                // Convert message to JSON.
                jsonMSG = msg.ConvertMessage();

                // Send data to server.
                Send(socket_client, jsonMSG);

                // Wait until send is done.
                sendDone.WaitOne();

                // Start a new thread with the KeepAlive function.
                keepAliveThread = new Thread(() => KeepAlive());
                keepAliveThread.Start();
                break;
            case "JOIN_SERVER":
                // Load new scene on main thread.
                LoadScene("MainMenu");
                break;
            case "GAME_FOUND":
                EGS_UpdateData gameData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.messageContent);

                // List of players at the game.
                //gameData.GetPlayersAtGame();

                room = gameData.GetRoom();

                msg = new EGS_Message();
                msg.messageType = "READY";
                msg.messageContent = "" + room;

                // Convert message to JSON.
                jsonMSG = msg.ConvertMessage();

                // Send data to server.
                Send(socket_client, jsonMSG);
                break;
            case "GAME_START":
                // Load new scene on main thread.
                LoadScene("TestGame");
                break;
            case "POSITION":
                Vector3 position = new Vector3();
                string[] stringPos = receivedMessage.messageContent.Split('|');

                float[] numericPos = new float[stringPos.Length];
                for (int i = 0; i < numericPos.Length; i++)
                    numericPos[i] = float.Parse(stringPos[i]);

                position.Set(numericPos[0], numericPos[1], numericPos[2]);
                EGS_CL_Sockets.playerPosition = position;
                break;
            default:
                Debug.Log("<color=yellow>Undefined message type: </color>" + receivedMessage.messageType);
                break;
        }

        // Keep receiving messages from the server.
        Receive(socket_client);
    }

    private void KeepAlive()
    {
        while (EGS_Client.connectedToServer)
        {
            // Tell the server client is still alive.
            EGS_Message msg = new EGS_Message();
            msg.messageType = "KEEP_ALIVE";
            msg.messageContent = "";
            string jsonMSG = msg.ConvertMessage();

            Send(socket_client, jsonMSG);

            Thread.Sleep(1000);
        }
    }

    #region MainThreadFunctions
    private void LoadScene(string sceneName)
    {
        switch (sceneName)
        {
            case "MainMenu":
                EGS_Dispatcher.RunOnMainThread(LoadMainMenu);
                break;
            case "TestGame":
                EGS_Dispatcher.RunOnMainThread(LoadGame);
                break;
            default:
                Debug.Log("<color=yellow>Undefined scene name: </color>" + sceneName);
                break;
        }
        
    }

    #region SceneLoads
    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void LoadGame()
    {
        SceneManager.LoadScene("TestGame");
    }
    #endregion
    #endregion
    #endregion
}