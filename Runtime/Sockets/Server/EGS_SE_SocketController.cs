using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Timers;
using System.Linq;

/// <summary>
/// Class EGS_SE_SocketController, that controls the server receiver socket.
/// </summary>
public class EGS_SE_SocketController
{
    #region Variables
    /// ManualResetEvents
    // ManualResetEvent allDone for connection.
    public ManualResetEvent allDone = new ManualResetEvent(false);

    /// References
    // Reference to the Log.
    private EGS_Log egs_Log = null;

    /// Delegates
    // Delegate to the OnNewConnection function.
    private Action<Socket> onNewConnection;
    // Delegate to the OnClientDisconnect function.
    private Action<Socket> onDisconnectDelegate;

    // List of timers.
    private Dictionary<Socket, System.Timers.Timer> socketTimeoutCounters = new Dictionary<Socket, System.Timers.Timer>();

    /// Users data.
    // All users.
    private Dictionary<string, EGS_User> allUsers = new Dictionary<string, EGS_User>();
    // Connected users.
    private Dictionary<Socket, EGS_User> connectedUsers = new Dictionary<Socket, EGS_User>();
    // Players in game.
    private Dictionary<string, EGS_Player> playersInGame = new Dictionary<string, EGS_Player>();

    // ID Assigner.
    private Mutex IDMutex = new Mutex();
    private int nextID = 0;

    /// Game servers.
    private Dictionary<Socket, int> gameServersAssignment = new Dictionary<Socket, int>();
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_SE_SocketController(EGS_Log log, Action<Socket> onNewConnection, Action<Socket> onClientDisconnect)
    {
        this.egs_Log = log;
        this.onNewConnection = onNewConnection;
        this.onDisconnectDelegate = onClientDisconnect;
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, that opens the socket to connections.
    /// </summary>
    /// <param name="serverPort">Port where the server is</param>
    /// <param name="remoteEP">EndPoint where the server is</param>
    /// <param name="socket_listener">Socket to use</param>
    public void StartListening(int serverPort, EndPoint localEP, Socket socket_listener)
    {
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            socket_listener.Bind(localEP);
            socket_listener.Listen(100);

            if (EGS_ServerManager.DEBUG_MODE > -1)
                egs_Log.Log("<color=green>Easy Game Server</color> Listening at port <color=orange>" + serverPort + "</color>.");

            // Start listening for connections asynchronously.
            socket_listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                socket_listener);
        }
        catch(ThreadAbortException)
        {
            //egs_Log.LogWarning("Aborted server thread");
        }
        catch (Exception e)
        {
            if (EGS_ServerManager.DEBUG_MODE > -1)
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

                if (EGS_ServerManager.DEBUG_MODE > 2)
                    egs_Log.Log("Keep receiving messages from: " + handler.RemoteEndPoint);
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
            if (EGS_ServerManager.DEBUG_MODE > 2)
                egs_Log.Log("Sent " + bytesSent + " bytes to client.");

        }
        catch (SocketException) { }
        catch (Exception e)
        {
            if (EGS_ServerManager.DEBUG_MODE > -1)
                egs_Log.LogError(e.ToString());
        }
    }

    private void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        EGS_Message receivedMessage = new EGS_Message();
        receivedMessage = JsonUtility.FromJson<EGS_Message>(content);

        if (EGS_ServerManager.DEBUG_MODE > 2)
            egs_Log.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);

        // Message to send back.
        EGS_Message msg = new EGS_Message();

        string jsonMSG;
        EGS_User receivedUser;
        EGS_User thisUser;
        int gameServerID;

        // Depending on the messageType, do different things
        switch (receivedMessage.messageType)
        {
            case "TEST_MESSAGE":
                if (EGS_ServerManager.DEBUG_MODE > 2)
                    egs_Log.Log("<color=purple>Data:</color> " + receivedMessage.messageContent);
                break;
            case "KEEP_ALIVE":
                if (EGS_ServerManager.DEBUG_MODE > 2)
                    egs_Log.Log("<color=purple>Keep alive:</color> " + connectedUsers[handler].GetUsername());
                socketTimeoutCounters[handler].Stop();
                socketTimeoutCounters[handler].Start();
                break;
            case "USER_JOIN_SERVER":
                // Get the received user
                receivedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
                receivedUser.SetSocket(handler);

                // Check if user is registered.
                if (!allUsers.ContainsKey(receivedUser.GetUsername()))
                {
                    // User has to be registered.
                    RegisterUser(receivedUser);
                }

                // Connect the user.
                ConnectUser(receivedUser, handler);

                // Put a heartbeat for the client socket.
                HeartbeatClient(handler);

                // Echo the data back to the client.
                msg = new EGS_Message();
                msg.messageType = "JOIN_SERVER";
                msg.messageContent = JsonUtility.ToJson(receivedUser);
                jsonMSG = msg.ConvertMessage();
                Send(handler, jsonMSG);
                break;
            case "DISCONNECT_USER":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Disconnect the user from the server.
                DisconnectUser(thisUser, handler);

                // Echo the disconnection back to the client.
                msg = new EGS_Message();
                msg.messageType = "DISCONNECT";
                jsonMSG = msg.ConvertMessage();
                Send(handler, jsonMSG);
                break;
            case "QUEUE_JOIN":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Add the player to the queue.
                EGS_Player newPlayer = new EGS_Player(thisUser);
                EGS_GamesManager.gm_instance.searchingGame_players.Enqueue(newPlayer);

                if (EGS_ServerManager.DEBUG_MODE > 0)
                    egs_Log.Log("Searching game: " + thisUser.GetUsername() + ".");

                CheckQueueToStartGame();
                break;
            case "QUEUE_LEAVE":
                bool isPlayerInQueue = false;

                // Lock the queue
                lock (EGS_GamesManager.gm_instance.searchingGame_players)
                {
                    // Check if player is in queue.
                    foreach (EGS_Player p in EGS_GamesManager.gm_instance.searchingGame_players)
                    {
                        if (p.GetUser().GetSocket() == handler)
                        {
                            isPlayerInQueue = true;
                            break;
                        }
                    }

                    if (isPlayerInQueue)
                    {
                        // Remove the player from the Queue by constructing a new queue based on the previous one but without the left player.
                        EGS_GamesManager.gm_instance.searchingGame_players =
                            new ConcurrentQueue<EGS_Player>(EGS_GamesManager.gm_instance.searchingGame_players.Where(x => x.GetUser().GetSocket() != handler));
                    }
                }
                break;
            case "DISCONNECT_TO_GAME":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Disconnect the user from the server.
                DisconnectUserToGame(thisUser, handler);

                // Echo the disconnection back to the client.
                msg = new EGS_Message();
                msg.messageType = "DISCONNECT_TO_GAME";
                jsonMSG = msg.ConvertMessage();
                Send(handler, jsonMSG);
                break;
            case "LEAVE_GAME":
                // Get the player.
                EGS_Player leftPlayer = playersInGame[receivedMessage.messageContent];
                playersInGame.Remove(receivedMessage.messageContent);

                EGS_GamesManager.gm_instance.QuitPlayerFromGame(leftPlayer);
                break;
            case "KEEP_ALIVE_GAME_SERVER":
                // Get game server status.
                string status = receivedMessage.messageContent;
                gameServerID = 0;
                // Get the gameServerID.
                try
                {
                    gameServerID = gameServersAssignment[handler];
                }
                catch (System.Collections.Generic.KeyNotFoundException)
                {
                    // Do nothing.
                }

                // Reset its timer.
                if (EGS_ServerManager.DEBUG_MODE > 2)
                    egs_Log.Log("<color=purple>Keep alive Game Server:</color> " + gameServerID + ". Status: " + status + ".");
                socketTimeoutCounters[handler].Stop();
                socketTimeoutCounters[handler].Start();

                // Set game server status.
                EGS_GamesManager.gm_instance.gameServers[gameServerID].Status = (EGS_GameServerData.State)Enum.Parse(typeof(EGS_GameServerData.State), status);

                /*TODO: No me gusta esto:
                 * // Depending on its status, execute code.
                switch (status)
                {
                    case "CREATED":
                        // TODO: Tell the GAME SERVER that the master server is going to tell the players to connect to it.
                        // Then, the GAME SERVER changes its status to waiting.
                        break;
                    case "WAITING_PLAYERS":
                        break;
                    case "STARTED":
                        break;
                    case "FINISHED":
                        break;
                }*/

                // TODO: Esto habría que verlo.
                // TODO: Tell the GAME SERVER that the master server is going to tell the players to connect to it.
                // Then, the GAME SERVER changes its status to waiting.

                break;
            case "CREATED_GAME_SERVER":
                // Get the message info.
                string[] messageInfo = receivedMessage.messageContent.Split('#');

                // Get the gameServerID.
                gameServerID = int.Parse(messageInfo[0]);

                lock (gameServersAssignment)
                {
                    gameServersAssignment.Add(handler, gameServerID);
                }

                // Put a heartbeat for the client socket.
                HeartbeatClient(handler);

                if (EGS_ServerManager.DEBUG_MODE > -1)
                    egs_Log.Log("<color=purple>Game Server created and connected</color>: " + gameServerID + ". IP: " + handler.RemoteEndPoint + ".");

                EGS_GamesManager.gm_instance.gameServers[gameServerID].Status = EGS_GameServerData.State.CREATED;

                string gameServerIP = messageInfo[1];

                // Send the game server IP to the players.
                msg = new EGS_Message();
                msg.messageType = "CHANGE_TO_GAME_SERVER";
                msg.messageContent = gameServerIP;

                jsonMSG = msg.ConvertMessage();

                int roomID = EGS_GamesManager.gm_instance.gameServers[gameServerID].Room_ID;

                foreach (EGS_Player p in EGS_GamesManager.gm_instance.playersInRooms[roomID])
                {
                    Send(p.GetUser().GetSocket(), jsonMSG);
                    if (EGS_ServerManager.DEBUG_MODE > 1)
                        egs_Log.Log("SENT ORDER TO: " + p.GetUser().GetUsername() + ".");
                }
                break;
            default:
                if (EGS_ServerManager.DEBUG_MODE > -1)
                    egs_Log.Log("<color=yellow>Undefined message type</color>: " + receivedMessage.messageType + ".");
                break;
        }
    }

    private void DisconnectUserToGame(EGS_User userToDisconnect, Socket client_socket)
    {
        // Disconnect the client.
        DisconnectClient(client_socket);

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Disconnected To Connect to the Game Server</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");
    }

    private void ConnectUser(EGS_User userToConnect, Socket client_socket)
    {
        // Set its user ID.
        userToConnect.SetUserID(allUsers[userToConnect.GetUsername()].GetUserID());

        // Save user data on the dictionary of connectedUsers.
        lock (connectedUsers)
        {
            connectedUsers.Add(client_socket, userToConnect);
        }

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Connected User</color>: UserID: " + userToConnect.GetUserID() + " - Username: " + userToConnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");
    }

    private void DisconnectUser(EGS_User userToDisconnect, Socket client_socket)
    {
        // Disconnect it from server.
        lock (connectedUsers)
        {
            connectedUsers.Remove(client_socket);
        }

        // Disconnect the client.
        DisconnectClient(client_socket);

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Disconnected User</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");
    }

    private void RegisterUser(EGS_User userToRegister)
    {
        // Assign an user ID.
        try
        {
            IDMutex.WaitOne();
            userToRegister.SetUserID(nextID);
            nextID++;
        }
        catch (Exception e)
        {
            if (EGS_ServerManager.DEBUG_MODE > -1)
                egs_Log.LogError(e.ToString());
        }
        finally
        {
            IDMutex.ReleaseMutex();
        }

        // Register it.
        lock (allUsers)
        {
            allUsers.Add(userToRegister.GetUsername(), userToRegister);
        }

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Registered User</color>: UserID: " + userToRegister.GetUserID() + " - Username: " + userToRegister.GetUsername() + ".");
    }

    private void DeleteUser(EGS_User userToDelete)
    {
        // Assing correct data to User.
        userToDelete.SetUserID(allUsers[userToDelete.GetUsername()].GetUserID());
        userToDelete.SetSocket(allUsers[userToDelete.GetUsername()].GetSocket());

        // If it is a manual user delete.
        lock (connectedUsers)
        {
            if (connectedUsers.ContainsKey(userToDelete.GetSocket()))
            {
                connectedUsers.Remove(userToDelete.GetSocket());
            }
        }

        // Remove it from server list.
        lock (allUsers)
        {
            allUsers.Remove(userToDelete.GetUsername());
        }

        // Send message to user.
        EGS_Message msg = new EGS_Message();
        msg.messageType = "DELETE_USER";
        msg.messageContent = JsonUtility.ToJson(userToDelete);
        Send(userToDelete.GetSocket(), msg.ConvertMessage());

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Deleted User</color>: UserID: " + userToDelete.GetUserID() + " - Username: " + userToDelete.GetUsername());
    }

    private void CheckQueueToStartGame()
    {
        bool areEnoughForAGame = false;
        List<EGS_Player> playersForThisGame = new List<EGS_Player>();

        // Lock to evit problems with the queue.
        lock (EGS_GamesManager.gm_instance.searchingGame_players)
        {
            // If there are enough players to start a game.
            if (EGS_GamesManager.gm_instance.searchingGame_players.Count >= EGS_GamesManager.gm_instance.PLAYERS_PER_GAME)
            {
                areEnoughForAGame = true;
                for (int i = 0; i < EGS_GamesManager.gm_instance.PLAYERS_PER_GAME; i++)
                {
                    // Get the player from the queue.
                    EGS_Player playerToGame;
                    EGS_GamesManager.gm_instance.searchingGame_players.TryDequeue(out playerToGame);

                    // Add the player to the dictionary and the list of this game.
                    playersInGame.Add(playerToGame.GetUser().GetUsername(), playerToGame);
                    playersForThisGame.Add(playerToGame);
                }
            }
        }

        // If there are enough players for a game:
        if (areEnoughForAGame)
        {
            // Construct the message to send.
            EGS_UpdateData updateData = new EGS_UpdateData();

            for (int i = 0; i < playersForThisGame.Count; i++)
            {
                EGS_PlayerData playerData = new EGS_PlayerData(i, playersForThisGame[i].GetUser().GetUsername());
                playersForThisGame[i].SetIngameID(i);
                updateData.GetPlayersAtGame().Add(playerData);
            }

            // Create the game and get the room number.
            int room = EGS_GamesManager.gm_instance.CreateGame(this, playersForThisGame);

            updateData.SetRoom(room);

            // Message for the players.
            EGS_Message msg = new EGS_Message();
            msg.messageType = "GAME_FOUND";
            msg.messageContent = JsonUtility.ToJson(updateData);

            string jsonMSG = msg.ConvertMessage();

            // Message the players so they know that found a game.
            foreach (EGS_Player p in playersForThisGame)
            {
                Send(p.GetUser().GetSocket(), jsonMSG);
            }
        }
    }

    public void DisconnectClient(Socket client_socket)
    {
        onDisconnectDelegate(client_socket);
        StopHeartbeat(client_socket);
    }
    private void HeartbeatClient(Socket client_socket)
    {
        socketTimeoutCounters.Add(client_socket, new System.Timers.Timer(15000));
        socketTimeoutCounters[client_socket].Start();
        socketTimeoutCounters[client_socket].Elapsed += (sender, e) => DisconnectClientByTimeout(sender, e, client_socket);
    }

    private void StopHeartbeat(Socket client_socket)
    {
        lock (socketTimeoutCounters)
        {
            socketTimeoutCounters[client_socket].Stop();
            socketTimeoutCounters[client_socket].Close();
            socketTimeoutCounters.Remove(client_socket);
        }
    }

    private void DisconnectClientByTimeout(object sender, ElapsedEventArgs e, Socket client_socket)
    {
        EGS_User userToDisconnect = connectedUsers[client_socket];
        DisconnectClient(client_socket);

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Disconnected User:</color> UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername());
        connectedUsers.Remove(client_socket);
    }

    private void TestMessage(Socket client_socket)
    {
        // Send the test message
        EGS_Message msg = new EGS_Message();
        msg.messageType = "TEST_MESSAGE";
        string jsonMSG = msg.ConvertMessage();

        Send(client_socket, jsonMSG);
    }
    #endregion
}