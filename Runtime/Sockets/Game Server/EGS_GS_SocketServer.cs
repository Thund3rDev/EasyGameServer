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
    // TODO: Move Delegates to a static class, singleton or global object.
    [Header("Delegates")]
    [Tooltip("Delegate to the OnNewConnection function")]
    private Action<Socket> onNewConnection;
    [Tooltip("Delegate to the OnClientDisconnect function")]
    private Action<Socket> onDisconnectDelegate;


    [Header("User data")]
    [Tooltip("Dictionary that stores ALL users by their username")] // TODO: Make this By ID.
    private Dictionary<string, EGS_User> usersToThisGame = new Dictionary<string, EGS_User>();
    [Tooltip("Dictionary that stores the CURRENTLY CONNECTED users by their socket")]
    private Dictionary<Socket, EGS_User> connectedUsers = new Dictionary<Socket, EGS_User>();

    [Tooltip("Dictionary that stores the timer by socket to check if still connected")]
    private Dictionary<Socket, System.Timers.Timer> socketTimeoutCounters = new Dictionary<Socket, System.Timers.Timer>();


    [Header("References")]
    [Tooltip("Reference to the sockets controller")]
    private EGS_GS_Sockets socketsController;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public EGS_GS_SocketServer(EGS_GS_Sockets socketsController_, Action<Socket> afterPlayerConnected, Action<Socket> afterPlayerDisconnect)
    {
        socketsController = socketsController_;
        onNewConnection = afterPlayerConnected;
        onDisconnectDelegate = afterPlayerDisconnect;

        // Get the info of users to this game.
        foreach (EGS_PlayerToGame playerToGame in EGS_GameServer.gameServer_instance.startData.GetPlayersToGame())
        {
            EGS_User thisUser = playerToGame.GetUser();
            usersToThisGame.Add(thisUser.GetUsername(), thisUser);

            // TODO: Value if this is needed.
            /*EGS_Player thisPlayer = new EGS_Player(thisUser, userToGame.GetIngameID());
            playersInGame.Add(thisUser.GetUsername(), thisPlayer);*/

            //EGS_GameServer.gameServer_instance.thisGame.AddPlayer();
            //egs_gameManager.GetPlayersInGame().Add(thisUser.GetUsername(), thisPlayer);
        }
    }
    #endregion

    #region Class Methods
    #region Public Methods
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
            socket_listener.Listen(4); // TODO: Listen up to MAX_PLAYERS.

            // Start listening for connections asynchronously.
            socket_listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                socket_listener);

            // TODO: Change this.
            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text = "LISTENING "; });
            socketsController.startDone.Set();
        }
        catch (ThreadAbortException) 
        {
            // TODO: Control this exception.
        }
        catch (Exception e)
        {
            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text = e.StackTrace; });
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

        // Put a heartbeat for the client socket.
        HeartbeatClient(handler); // TODO: Check if this should be here or later, as client.

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
    #endregion

    #region Private Methods
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
        }
        catch (SocketException)
        {
            // TODO: Control this Exception.
        }
        catch (Exception e)
        {
            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text = e.StackTrace; });
        }
    }

    /// <summary>
    /// Method HandleMessage, that receives a message from a client and do things based on it.
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

        // Message to send back.
        EGS_Message messageToSend = new EGS_Message();

        // Local variables that are used in the cases below.
        string jsonMSG;
        EGS_User receivedUser;
        EGS_Player thisPlayer;

        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "KEEP_ALIVE":
                socketTimeoutCounters[handler].Stop();
                socketTimeoutCounters[handler].Start();
                break;
            case "JOIN_GAME_SERVER":
                try
                {
                    // Get the received user
                    receivedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
                    receivedUser.SetSocket(handler);

                    // If the user is on the list to play this game.
                    if (usersToThisGame.ContainsKey(receivedUser.GetUsername()))
                    {
                        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nPLAYER JOINED: " + receivedUser.GetUsername(); });
                            
                        // Connect the user.
                        ConnectUser(receivedUser, handler);

                        // TODO: Check if Heartbeat should be here.

                        // Echo the data back to the client.
                        messageToSend.messageType = "JOIN_GAME_SERVER";
                        jsonMSG = messageToSend.ConvertMessage();
                        Send(handler, jsonMSG);

                        // Check if game started / are all players.
                        // TODO: Only prepare the game, not start it.
                        bool startedGame = EGS_GameServer.gameServer_instance.thisGame.Ready();
                        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nStartedGame: " + startedGame; });

                        if (startedGame)
                        {
                            // TODO: Send to the master server the info of the started game.

                            messageToSend = new EGS_Message();
                            messageToSend.messageType = "GAME_START";
                            messageToSend.messageContent = "";

                            jsonMSG = messageToSend.ConvertMessage();

                            string playersString = "";
                            foreach(EGS_PlayerToGame player in EGS_GameServer.gameServer_instance.startData.GetPlayersToGame())
                            {
                                playersString += player.GetUser().GetUsername() + ", ";
                            }

                            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\n" + EGS_GameServer.gameServer_instance.thisGame.GetPlayers().Count + " | " + playersString; });
                            foreach (EGS_PlayerToGame player in EGS_GameServer.gameServer_instance.startData.GetPlayersToGame())
                            {
                                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nSEND TO : " + player.GetUser().GetUsername(); });
                                Send(player.GetUser().GetSocket(), jsonMSG);
                            }

                            // TODO: Escena jugable
                            // TODO: Delegates.
                            LoadScene("TestGame");
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
                // TODO: Input object and delegate.
                // Get the input data
                // Inputs[0] = userName | Inputs[1-4] = directions.
                string[] inputs = receivedMessage.messageContent.Split(',');

                bool[] realInputs = new bool[4];
                for (int i = 0; i < realInputs.Length; i++)
                    realInputs[i] = bool.Parse(inputs[i + 1]);

                // Get the player from its ingameID.
                thisPlayer = EGS_GameManager.instance.GetPlayersByID()[int.Parse(inputs[0])];

                // Assign its inputs.
                thisPlayer.SetInputs(realInputs);
                break;
            case "LEAVE_GAME":
                // Get the player.
                EGS_Player leftPlayer = EGS_GameManager.instance.GetPlayersByID()[int.Parse(receivedMessage.messageContent)];
                EGS_GameManager.instance.GetPlayersByID().Remove(int.Parse(receivedMessage.messageContent));

                EGS_GameServer.gameServer_instance.thisGame.QuitPlayerFromGame(leftPlayer);
                break;
            default:
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nUndefined message type: " + receivedMessage.messageType; });
                break;
        }
    }

    #region User Management Methods
    /// <summary>
    /// Method ConnectUser, that connects an user to the game server.
    /// </summary>
    /// <param name="userToConnect">User to connect to the server</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    private void ConnectUser(EGS_User userToConnect, Socket client_socket)
    {
        // Update its socket.
        usersToThisGame[userToConnect.GetUsername()].SetSocket(client_socket);

        // Set its user ID.
        userToConnect.SetUserID(usersToThisGame[userToConnect.GetUsername()].GetUserID());

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

        client_socket.Shutdown(SocketShutdown.Both);
        client_socket.Close();

        lock (socketTimeoutCounters)
        {
            socketTimeoutCounters[client_socket].Stop();
            socketTimeoutCounters[client_socket].Close();
            socketTimeoutCounters.Remove(client_socket);
        }
    }

    /// <summary>
    /// Method HeartbeatClient, that puts a timer to check if clients are still connected.
    /// </summary>
    /// <param name="client_socket">Socket that handles the client</param>
    private void HeartbeatClient(Socket client_socket)
    {
        socketTimeoutCounters.Add(client_socket, new System.Timers.Timer(3000));
        socketTimeoutCounters[client_socket].Start();
        socketTimeoutCounters[client_socket].Elapsed += (sender, e) => DisconnectClientByTimeout(sender, e, client_socket);
    }

    /// <summary>
    /// Method StopHeartbeat, that stops the timer assigned to the client socket.
    /// </summary>
    /// <param name="client_socket">Socket that handles the client</param>
    private void StopHeartbeat(Socket client_socket)
    {
        lock (socketTimeoutCounters)
        {
            socketTimeoutCounters[client_socket].Stop();
            socketTimeoutCounters[client_socket].Close();
            socketTimeoutCounters.Remove(client_socket);
        }
    }

    /// <summary>
    /// Method DisconnectClientByTimeout, to disconnect a client when the timer was completed.
    /// </summary>
    /// <param name="sender">Object needed by the timer</param>
    /// <param name="e">ElapsedEventArgs needed by the timer</param>
    /// <param name="client_socket">Socket that handles the client</param>
    private void DisconnectClientByTimeout(object sender, ElapsedEventArgs e, Socket client_socket)
    {
        EGS_User userToDisconnect = connectedUsers[client_socket];
        DisconnectClient(client_socket);

        connectedUsers.Remove(client_socket);

    }
    #endregion

    #region MainThreadFunctions
    /// <summary>
    /// Method LoadScene, to load a scene on the main thread.
    /// </summary>
    /// <param name="sceneName">Scene name</param>
    private void LoadScene(string sceneName)
    {
        EGS_Dispatcher.RunOnMainThread(() => { SceneManager.LoadScene(sceneName); });
    }
    #endregion
    #endregion
    #endregion
}