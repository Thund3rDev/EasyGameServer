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
/// Class EGS_SE_SocketServer, that controls the server listener socket.
/// </summary>
public class EGS_SE_SocketServer
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
    private Dictionary<string, EGS_User> allUsers = new Dictionary<string, EGS_User>();
    [Tooltip("Dictionary that stores the CURRENTLY CONNECTED users by their socket")]
    private Dictionary<Socket, EGS_User> connectedUsers = new Dictionary<Socket, EGS_User>();

    [Tooltip("Dictionary that stores the timer by socket to check if still connected")]
    private Dictionary<Socket, System.Timers.Timer> socketTimeoutCounters = new Dictionary<Socket, System.Timers.Timer>();


    [Header("ID Assigner")]
    [Tooltip("Mutex that controls the concurrency to assign new users IDs")]
    private Mutex IDMutex = new Mutex();
    [Tooltip("Integer that stores the next created user ID")]
    private int nextID = 0;

    [Header("Game servers")]
    [Tooltip("Dictionary that stores the Game Servers by their socket")] // TODO: Check if this is needed and move it to the EGS_ServerGamesManager.
    private Dictionary<Socket, int> gameServersAssignment = new Dictionary<Socket, int>();

    [Header("References")]
    [Tooltip("Reference to the Log")]
    private EGS_Log egs_Log = null;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public EGS_SE_SocketServer(EGS_Log log, Action<Socket> onNewConnection, Action<Socket> onClientDisconnect)
    {
        this.egs_Log = log;
        this.onNewConnection = onNewConnection;
        this.onDisconnectDelegate = onClientDisconnect;
    }
    #endregion

    #region Class Methods
    #region Public Methods
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
            socket_listener.Listen(100); // TODO: Listen up to MAX_CONNECTIONS.

            if (EGS_ServerManager.DEBUG_MODE > -1)
                egs_Log.Log("<color=green>Easy Game Server</color> Listening at port <color=orange>" + serverPort + "</color>.");

            // Start listening for connections asynchronously.
            socket_listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                socket_listener);
        }
        catch(ThreadAbortException)
        {
            //egs_Log.LogWarning("Aborted server thread"); // TODO: Control this Exception.
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
            if (EGS_ServerManager.DEBUG_MODE > 2)
                egs_Log.Log("Sent " + bytesSent + " bytes to client.");

        }
        catch (SocketException) {
            // TODO: Control this Exception.
        }
        catch (Exception e)
        {
            if (EGS_ServerManager.DEBUG_MODE > -1)
                egs_Log.LogError(e.ToString());
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

        if (EGS_ServerManager.DEBUG_MODE > 2)
            egs_Log.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);

        // Message to send back.
        EGS_Message messageToSend = new EGS_Message();

        // Local variables that are used in the cases below.
        string jsonMSG;
        EGS_User thisUser;
        int gameServerID;

        // Depending on the messageType, do different things.
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
                // Get the received user.
                thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
                thisUser.SetSocket(handler);

                // Check if user is registered.
                if (!allUsers.ContainsKey(thisUser.GetUsername()))
                {
                    // User has to be registered.
                    RegisterUser(thisUser);
                }

                // Connect the user.
                ConnectUser(thisUser, handler);

                // Put a heartbeat for the client socket.
                HeartbeatClient(handler);

                // Echo the data back to the client.
                messageToSend.messageType = "JOIN_SERVER";
                messageToSend.messageContent = JsonUtility.ToJson(thisUser);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);
                break;
            case "DISCONNECT_USER":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Disconnect the user from the server.
                DisconnectUser(thisUser, handler);

                // Echo the disconnection back to the client.
                messageToSend.messageType = "DISCONNECT";
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);
                break;
            case "QUEUE_JOIN":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Add the player to the queue.
                EGS_PlayerToGame newPlayer = new EGS_PlayerToGame(thisUser);
                EGS_ServerGamesManager.gm_instance.searchingGame_players.Enqueue(newPlayer);

                if (EGS_ServerManager.DEBUG_MODE > 0)
                    egs_Log.Log("Searching game: " + thisUser.GetUsername() + ".");

                // Check the Queue to Start Game.
                CheckQueueToStartGame();
                break;
            case "QUEUE_LEAVE":
                // Bool to know if player is in queue.
                bool isPlayerInQueue = false;

                // Lock the queue.
                lock (EGS_ServerGamesManager.gm_instance.searchingGame_players)
                {
                    // Check if player is in queue.
                    foreach (EGS_PlayerToGame playerInQueue in EGS_ServerGamesManager.gm_instance.searchingGame_players)
                    {
                        if (playerInQueue.GetUser().GetSocket() == handler)
                        {
                            isPlayerInQueue = true;
                            break;
                        }
                    }

                    if (isPlayerInQueue)
                    {
                        // Remove the player from the Queue by constructing a new queue based on the previous one but without the left player.
                        EGS_ServerGamesManager.gm_instance.searchingGame_players =
                            new ConcurrentQueue<EGS_PlayerToGame>(EGS_ServerGamesManager.gm_instance.searchingGame_players.Where(x => x.GetUser().GetSocket() != handler));
                    }
                }
                break;
            case "DISCONNECT_TO_GAME":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Disconnect the user from the server.
                DisconnectUserToGame(thisUser, handler);

                // Echo the disconnection back to the client.
                messageToSend.messageType = "DISCONNECT_TO_GAME";
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);
                break;
            case "LEAVE_GAME":
                // TODO: Get the information from the GameServer about the player who left.
                //  Check if was leave closing or leave to the master server.

                // Get the player.
                //EGS_Player leftPlayer = playersInGame[receivedMessage.messageContent];
                //playersInGame.Remove(receivedMessage.messageContent);

                //EGS_ServerGamesManager.gm_instance.QuitPlayerFromGame(leftPlayer);
                break;
            case "KEEP_ALIVE_GAME_SERVER":
                // Get game server status.
                string status = receivedMessage.messageContent;
                // TODO: Receive the ID so don't have to take it from the dictionary.
                
                // Get the gameServerID.
                try
                {
                    gameServerID = gameServersAssignment[handler];
                }
                catch (KeyNotFoundException)
                {
                    // TODO: Check why can this happen.
                    gameServerID = -1;
                }

                // Reset its timer.
                if (EGS_ServerManager.DEBUG_MODE > 2)
                    egs_Log.Log("<color=purple>Keep alive Game Server:</color> " + gameServerID + ". Status: " + status + ".");

                socketTimeoutCounters[handler].Stop();
                socketTimeoutCounters[handler].Start();

                // Set game server status.
                EGS_ServerGamesManager.gm_instance.gameServers[gameServerID].SetStatus((EGS_GameServerData.EGS_GameServerState)Enum.Parse(typeof(EGS_GameServerData.EGS_GameServerState), status));

                // TODO: Show this status on the Server UI and make the status changes happen.
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

                // Get the game server IP from the message.
                // TODO: Maybe get it from the handler.
                string gameServerIP = messageInfo[1];

                if (EGS_ServerManager.DEBUG_MODE > -1)
                    egs_Log.Log("<color=purple>Game Server created and connected</color>: " + gameServerID + ". IP: " + gameServerIP + ".");

                // Assign the created status.
                EGS_ServerGamesManager.gm_instance.gameServers[gameServerID].SetStatus(EGS_GameServerData.EGS_GameServerState.CREATED);

                // Send the game server IP to the players.
                messageToSend.messageType = "CHANGE_TO_GAME_SERVER";
                messageToSend.messageContent = gameServerIP;
                jsonMSG = messageToSend.ConvertMessage();

                int roomID = EGS_ServerGamesManager.gm_instance.gameServers[gameServerID].GetRoom();

                foreach (EGS_User user in EGS_ServerGamesManager.gm_instance.usersInRooms[roomID])
                {
                    Send(user.GetSocket(), jsonMSG);

                    if (EGS_ServerManager.DEBUG_MODE > 1)
                        egs_Log.Log("SENT ORDER TO: " + user.GetUsername() + ".");
                }
                break;
            default:
                if (EGS_ServerManager.DEBUG_MODE > -1)
                    egs_Log.Log("<color=yellow>Undefined message type</color>: " + receivedMessage.messageType + ".");
                break;
        }
    }

    #region User Management Methods
    /// <summary>
    /// Method DisconnectUserToGame, that disconnect an user's client so it can connect to the game server.
    /// </summary>
    /// <param name="userToDisconnect">User who disconnects for the game</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    private void DisconnectUserToGame(EGS_User userToDisconnect, Socket client_socket)
    {
        // Disconnect the client.
        DisconnectClient(client_socket);

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Disconnected To Connect to the Game Server</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");
    }

    /// <summary>
    /// Method ConnectUser, that connects an user to the server.
    /// </summary>
    /// <param name="userToConnect">User to connect to the server</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
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

    /// <summary>
    /// Method DisconnectUser, that disconnects an user from the server.
    /// </summary>
    /// <param name="userToDisconnect">User to disconnect from the server</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
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

    /// <summary>
    /// Method RegisterUser, that registers an user in the server.
    /// </summary>
    /// <param name="userToRegister"></param>
    private void RegisterUser(EGS_User userToRegister)
    {
        // Assign an user ID.
        try
        {
            IDMutex.WaitOne();
            userToRegister.SetUserID(nextID);
            Interlocked.Increment(ref nextID);
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

        // TODO: Save on a database? Delegate.

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Registered User</color>: UserID: " + userToRegister.GetUserID() + " - Username: " + userToRegister.GetUsername() + ".");
    }

    /// <summary>
    /// Method DeleteUser, that deletes an user from the server.
    /// </summary>
    /// <param name="userToDelete">User to be deleted from the server</param>
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

        // TODO: Delete from a database? Delegate.

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Deleted User</color>: UserID: " + userToDelete.GetUserID() + " - Username: " + userToDelete.GetUsername());
    }
    #endregion

    // TODO: Pass this to the EGS_ServerGamesManager.
    /// <summary>
    /// Method CheckQueueToStartGame, that check if there are enough players in queue to start a game.
    /// </summary>
    private void CheckQueueToStartGame()
    {
        bool areEnoughForAGame = false;
        List<EGS_PlayerToGame> playersForThisGame = new List<EGS_PlayerToGame>();

        // Lock to evit problems with the queue.
        lock (EGS_ServerGamesManager.gm_instance.searchingGame_players)
        {
            // If there are enough players to start a game.
            if (EGS_ServerGamesManager.gm_instance.searchingGame_players.Count >= EGS_ServerGamesManager.gm_instance.PLAYERS_PER_GAME)
            {
                areEnoughForAGame = true;
                for (int i = 0; i < EGS_ServerGamesManager.gm_instance.PLAYERS_PER_GAME; i++)
                {
                    // Get the player from the queue.
                    EGS_PlayerToGame playerToGame;
                    EGS_ServerGamesManager.gm_instance.searchingGame_players.TryDequeue(out playerToGame);

                    // Add the player to the list of this game.
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
            int room = EGS_ServerGamesManager.gm_instance.CreateGame(playersForThisGame);

            // Get a list of the users to the game.
            List<EGS_User> usersToGame = new List<EGS_User>();

            foreach (EGS_PlayerToGame playerToGame in playersForThisGame)
            {
                playerToGame.GetUser().SetRoom(room);
                usersToGame.Add(playerToGame.GetUser());
            }

            // Save the users in the room.
            EGS_ServerGamesManager.gm_instance.usersInRooms.Add(room, usersToGame);

            updateData.SetRoom(room);

            // Message for the players.
            EGS_Message msg = new EGS_Message();
            msg.messageType = "GAME_FOUND";
            msg.messageContent = JsonUtility.ToJson(updateData);

            string jsonMSG = msg.ConvertMessage();

            // Set the room and message the users so they know that found a game.
            foreach (EGS_User userToGame in usersToGame)
            {
                Send(userToGame.GetSocket(), jsonMSG);
            }
        }
    }

    #region Networking Methods
    /// <summary>
    /// Method DisconnectClient, that disconnect a client from the server.
    /// </summary>
    /// <param name="client_socket">Socket that handles the client</param>
    public void DisconnectClient(Socket client_socket)
    {
        onDisconnectDelegate(client_socket);
        StopHeartbeat(client_socket);
    }

    /// <summary>
    /// Method HeartbeatClient, that puts a timer to check if clients are still connected.
    /// </summary>
    /// <param name="client_socket">Socket that handles the client</param>
    private void HeartbeatClient(Socket client_socket)
    {
        // TODO: Change the time from 15000 to TIME_TO_DISCONNECT.
        socketTimeoutCounters.Add(client_socket, new System.Timers.Timer(15000));
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
        // Disconnect the client socket.
        EGS_User userToDisconnect = connectedUsers[client_socket];
        DisconnectClient(client_socket);

        // Display data on the console.
        if (EGS_ServerManager.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Disconnected User:</color> UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername());

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