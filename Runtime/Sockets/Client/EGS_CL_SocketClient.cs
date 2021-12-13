using System;
using System.Collections.Generic;
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

    // SocketsController
    private EGS_CL_Sockets socketsController;

    // Test.
    int room = -1;
    string serverIP = "";
    int serverPort = -1;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_CL_SocketClient(EGS_CL_Sockets socketsController_)
    {
        socketsController = socketsController_;
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

            int bytesRead = 0;

            if (EGS_Client.connectedToMasterServer || EGS_Client.connectedToGameServer)
            {
                try
                {
                    bytesRead = client.EndReceive(ar);
                }
                catch (ObjectDisposedException) {}
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
        catch (ThreadAbortException) {}
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
            if (EGS_ServerManager.DEBUG_MODE > 2)
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
        try
        {
            receivedMessage = JsonUtility.FromJson<EGS_Message>(content);
        }
        catch (Exception e)
        {
            Debug.LogWarning("ERORR, CONTENT: " + content);
            throw e;
        }
        

        if (EGS_ServerManager.DEBUG_MODE > 2)
            Debug.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);

        // Message to send.
        EGS_Message msg;
        string jsonMSG;
        string userJson;

        // Depending on the messageType, do different things
        switch (receivedMessage.messageType)
        {
            case "TEST_MESSAGE":
                Debug.Log("Received test message from master server: " + receivedMessage.messageContent);
                break;
            case "CONNECT_TO_MASTER_SERVER":
                // Test data.
                EGS_User thisUser = new EGS_User();
                thisUser.SetUsername(EGS_Client.client_instance.username);

                // Convert user to JSON.
                userJson = JsonUtility.ToJson(thisUser);

                msg = new EGS_Message();
                msg.messageType = "USER_JOIN_SERVER";
                msg.messageContent = userJson;

                // Convert message to JSON.
                jsonMSG = msg.ConvertMessage();

                // Send data to server.
                Send(socket_client, jsonMSG);

                // Wait until send is done.
                sendDone.WaitOne();

                //EGS_Client.connectedToMasterServer = true;

                // Start a new thread with the KeepAlive function.
                keepAliveThread = new Thread(() => KeepAlive());
                keepAliveThread.Start();
                break;
            case "DISCONNECT":
                // Close the socket to disconnect from the server.
                socketsController.CloseSocket();

                // Change scene to the MainMenu.
                LoadScene("MainMenu");
                break;
            case "JOIN_SERVER":
                // Get User Data
                socketsController.thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);

                // Load new scene on main thread.
                LoadScene("GameMenu");
                break;
            case "GAME_FOUND":
                // Change scene to the GameLobby.
                LoadScene("GameLobby");

                EGS_UpdateData gameData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.messageContent);

                // Clear the dictionaries and add the new players.
                EGS_CL_Sockets.playerPositions.Clear();
                EGS_CL_Sockets.playerUsernames.Clear();
                foreach (EGS_PlayerData playerData in gameData.GetPlayersAtGame())
                {
                    EGS_CL_Sockets.playerPositions.Add(playerData.GetIngameID(), playerData.GetPosition());
                    EGS_CL_Sockets.playerUsernames.Add(playerData.GetIngameID(), playerData.GetUsername());
                }

                room = gameData.GetRoom();
                break;
            case "CHANGE_TO_GAME_SERVER":
                // Construct the EndPoint to Game Server.
                string[] ep = receivedMessage.messageContent.Split(':');
                serverIP = ep[0];
                serverPort = int.Parse(ep[1]);

                EGS_Dispatcher.RunOnMainThread(() => { Debug.Log("Game Server IP: " + receivedMessage.messageContent); });

                // Tell the server that the client received the information so can connect to the game server.
                msg = new EGS_Message();
                msg.messageType = "DISCONNECT_TO_GAME";

                // Convert message to JSON.
                jsonMSG = msg.ConvertMessage();

                // Send data to server.
                Send(socket_client, jsonMSG);

                break;
            case "DISCONNECT_TO_GAME":
                // Close the socket to disconnect from the server.
                socketsController.CloseSocket();
                EGS_Client.connectedToMasterServer = false;

                // Try to connect to Game Server.
                socketsController.ConnectToGameServer(serverIP, serverPort);
                break;
            case "CONNECT_GAME_SERVER":
                // Convert user to JSON.
                userJson = JsonUtility.ToJson(socketsController.thisUser);

                msg = new EGS_Message();
                msg.messageType = "JOIN_GAME_SERVER";
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
            case "JOIN_GAME_SERVER":
                // TODO: LoadGameScene, don't start game.
                //LoadScene("TestGame");
                break;
            case "GAME_START":
                // Load new scene on main thread.
                LoadScene("TestGame");
                break;
            case "UPDATE":
                Debug.Log("Update MSG: " + receivedMessage.messageContent);
                EGS_UpdateData updateData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.messageContent);

                foreach (EGS_PlayerData playerData in updateData.GetPlayersAtGame())
                {
                    EGS_CL_Sockets.playerPositions[playerData.GetIngameID()] = playerData.GetPosition();
                }
                break;
            default:
                Debug.Log("<color=yellow>Undefined message type: </color>" + receivedMessage.messageType);
                break;
        }
    }

    private void KeepAlive()
    {
        while (EGS_Client.connectedToMasterServer || EGS_Client.connectedToGameServer)
        {
            // Tell the server client is still alive.
            EGS_Message msg = new EGS_Message();
            msg.messageType = "KEEP_ALIVE";
            msg.messageContent = "";
            string jsonMSG = msg.ConvertMessage();

            Send(socket_client, jsonMSG);

            //EGS_Dispatcher.RunOnMainThread(() => { Debug.Log("KEEP ALIVE"); });

            Thread.Sleep(1000);
        }
    }

    #region MainThreadFunctions
    private void LoadScene(string sceneName)
    {
        EGS_Dispatcher.RunOnMainThread(() => { SceneManager.LoadScene(sceneName); });
    }
    #endregion
    #endregion
}