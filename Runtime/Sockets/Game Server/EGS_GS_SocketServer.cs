using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using System.Timers;
using UnityEngine.SceneManagement;

/// <summary>
/// Class EGS_GS_SocketServer, that controls the server receiver socket.
/// </summary>
public class EGS_GS_SocketServer
{
    #region Variables
    /// ManualResetEvents
    // ManualResetEvent allDone for connection.
    public ManualResetEvent allDone = new ManualResetEvent(false);

    /// References

    /// Delegates
    // Delegate to the OnNewConnection function.
    private Action<Socket> onConnectDelegate;
    // Delegate to the OnClientDisconnect function.
    private Action<Socket> onDisconnectDelegate;

    // List of timers.
    private Dictionary<Socket, System.Timers.Timer> socketTimeoutCounters = new Dictionary<Socket, System.Timers.Timer>();

    /// Users data.
    // Users to this game.
    private Dictionary<string, EGS_User> usersToThisGame = new Dictionary<string, EGS_User>();
    // Connected users.
    private Dictionary<Socket, EGS_User> connectedUsers = new Dictionary<Socket, EGS_User>();
    // Players in game.
    private Dictionary<string, EGS_Player> playersInGame = new Dictionary<string, EGS_Player>();

    // Concurrency

    // Sockets controller.
    EGS_GS_Sockets socketsController;

    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_GS_SocketServer(EGS_GS_Sockets socketsController_, Action<Socket> afterPlayerConnected, Action<Socket> afterPlayerDisconnect)
    {
        socketsController = socketsController_;
        onConnectDelegate = afterPlayerConnected;
        onDisconnectDelegate = afterPlayerDisconnect;

        // Get the info of users to this game.
        foreach (EGS_UserToGame userToGame in EGS_GameServer.gameServer_instance.startData.GetUsersToGame())
        {
            EGS_User thisUser = userToGame.GetUser();
            usersToThisGame.Add(thisUser.GetUsername(), thisUser);

            EGS_Player thisPlayer = new EGS_Player(thisUser, userToGame.GetIngameID());
            playersInGame.Add(thisUser.GetUsername(), thisPlayer);
        }
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, that opens the socket to connections.
    /// </summary>
    /// <param name="remoteEP">EndPoint where the server is</param>
    /// <param name="socket_listener">Socket to use</param>
    public void StartListening(EndPoint localEP, Socket socket_listener)
    {
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            socket_listener.Bind(localEP);
            socket_listener.Listen(4);

            // Start listening for connections asynchronously.
            socket_listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                socket_listener);

            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text = "LISTENING "; });
            socketsController.startDone.Set();
        }
        catch (ThreadAbortException) {}
        catch (Exception e)
        {
            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text = e.StackTrace; });
            //egs_Log.LogError(e.ToString());
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

        // Do things on client connected.
        onConnectDelegate(handler);

        // Put a heartbeat for the client socket.
        HeartbeatClient(handler);

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
    public void ReadCallback(IAsyncResult ar)
    {
        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
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
            /*if (EGS_ServerManager.DEBUG_MODE > 1)
                egs_Log.Log("Sent " + bytesSent + " bytes to client.");*/

        }
        catch (SocketException) {}
        catch (Exception)
        {
            //egs_Log.LogError(e.ToString());
        }
    }

    private void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        EGS_Message receivedMessage = new EGS_Message();
        receivedMessage = JsonUtility.FromJson<EGS_Message>(content);

        /*if (EGS_ServerManager.DEBUG_MODE > 1)
            egs_Log.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);*/

        // Message to send back.
        EGS_Message msg = new EGS_Message();

        string jsonMSG;
        EGS_User receivedUser;
        EGS_Player thisPlayer;

        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "KEEP_ALIVE":
                /*if (EGS_ServerManager.DEBUG_MODE > 2)
                    egs_Log.Log("<color=purple>Keep alive:</color> " + connectedUsers[handler].GetUsername());*/
                socketTimeoutCounters[handler].Stop();
                socketTimeoutCounters[handler].Start();
                break;
            case "JOIN_GAME_SERVER":
                try
                {
                    // Get the received user
                    receivedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
                    //EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nPLAYER xd: " + receivedUser.GetUsername(); });
                    receivedUser.SetSocket(handler);

                    // If the user is on the list to play this game.
                    if (usersToThisGame.ContainsKey(receivedUser.GetUsername()))
                    {
                        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nPLAYER JOINED: " + receivedUser.GetUsername(); });
                            
                        // Connect the user.
                        ConnectUser(handler, receivedUser);

                        // Create its player and save it.
                        thisPlayer = playersInGame[receivedUser.GetUsername()];
                        thisPlayer.SetRoom(EGS_GameServer.gameServer_instance.thisGame.GetRoom());

                        lock (EGS_GameServer.gameServer_instance.thisGame.GetPlayers())
                        {
                            EGS_GameServer.gameServer_instance.thisGame.AddPlayer(thisPlayer);
                        }

                        // Echo the data back to the client.
                        msg.messageType = "JOIN_GAME_SERVER";
                        jsonMSG = msg.ConvertMessage();
                        Send(handler, jsonMSG);

                        bool startedGame = EGS_GameServer.gameServer_instance.thisGame.Ready();
                        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nStartedGame: " + startedGame; });

                        if (startedGame)
                        {
                            // TODO: Send to the master server the info of the started game.

                            msg = new EGS_Message();
                            msg.messageType = "GAME_START";
                            msg.messageContent = "";

                            jsonMSG = msg.ConvertMessage();

                            string playersString = "";
                            foreach(EGS_Player player in EGS_GameServer.gameServer_instance.thisGame.GetPlayers())
                            {
                                playersString += player.GetUser().GetUsername() + ", ";
                            }

                            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\n" + EGS_GameServer.gameServer_instance.thisGame.GetPlayers().Count + " | " + playersString; });
                            foreach (EGS_Player p in EGS_GameServer.gameServer_instance.thisGame.GetPlayers())
                            {
                                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nSEND TO : " + p.GetUser().GetUsername(); });
                                Send(p.GetUser().GetSocket(), jsonMSG);
                            }

                            // TODO: Escena jugable
                            LoadScene("GameOnServer");
                        }
                    }

                }
                catch (Exception e)
                {
                    EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nEXCEPTION: " + e.ToString(); });
                }
                break;
            case "DISCONNECT_USER":
                // Get the received user
                receivedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);

                DisconnectUser(receivedUser);
                break;
            case "INPUT":
                // Get the input data
                // Inputs[0] = userName | Inputs[1-4] = directions.
                string[] inputs = receivedMessage.messageContent.Split(',');

                bool[] realInputs = new bool[4];
                for (int i = 0; i < realInputs.Length; i++)
                    realInputs[i] = bool.Parse(inputs[i + 1]);

                // Get the player from its username.
                thisPlayer = playersInGame[inputs[0]];

                // Assign its inputs.
                thisPlayer.SetInputs(realInputs);
                break;
            case "LEAVE_GAME":
                // Get the player.
                EGS_Player leftPlayer = playersInGame[receivedMessage.messageContent];
                playersInGame.Remove(receivedMessage.messageContent);

                EGS_GameServer.gameServer_instance.thisGame.QuitPlayerFromGame(leftPlayer);
                break;
            default:
                //egs_Log.Log("<color=yellow>Undefined message type: </color>" + receivedMessage.messageType);
                break;
        }
    }

    private void ConnectUser(Socket handler, EGS_User userToConnect)
    {
        // Update its socket.
        usersToThisGame[userToConnect.GetUsername()].SetSocket(handler);

        // Set its user ID.
        userToConnect.SetUserID(usersToThisGame[userToConnect.GetUsername()].GetUserID());

        // Save user data on the dictionary of connectedUsers.
        lock (connectedUsers)
        {
            connectedUsers.Add(handler, userToConnect);
        }
    }

    private void DisconnectUser(EGS_User userToDisconnect)
    {
        // Get the socket.
        userToDisconnect.SetSocket(usersToThisGame[userToDisconnect.GetUsername()].GetSocket());

        // Disconnect it from server.
        lock (connectedUsers)
        {
            connectedUsers.Remove(userToDisconnect.GetSocket());
        }

        // Disconnect the client.
        DisconnectClient(userToDisconnect.GetSocket());

        // Display data on the console.
        //egs_Log.Log("<color=purple>Disconnected User:</color> UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername());
    }

    public void DisconnectClient(Socket client_socket)
    {
        onDisconnectDelegate(client_socket);

        client_socket.Shutdown(SocketShutdown.Both);
        client_socket.Close();

        lock (socketTimeoutCounters)
        {
            socketTimeoutCounters[client_socket].Stop();
            socketTimeoutCounters[client_socket].Close();
            socketTimeoutCounters.Remove(client_socket);
        }
    }
    private void HeartbeatClient(Socket client_socket)
    {
        socketTimeoutCounters.Add(client_socket, new System.Timers.Timer(3000));
        socketTimeoutCounters[client_socket].Start();
        socketTimeoutCounters[client_socket].Elapsed += (sender, e) => DisconnectClientByTimeout(sender, e, client_socket);
    }

    private void DisconnectClientByTimeout(object sender, ElapsedEventArgs e, Socket client_socket)
    {
        EGS_User userToDisconnect = connectedUsers[client_socket];
        DisconnectClient(client_socket);

        // Display data on the console.
        //egs_Log.Log("<color=purple>Disconnected User:</color> UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername());
        connectedUsers.Remove(client_socket);

    }

    #region MainThreadFunctions
    private void LoadScene(string sceneName)
    {
        EGS_Dispatcher.RunOnMainThread(() => { SceneManager.LoadScene(sceneName); });
    }
    #endregion
    #endregion
}