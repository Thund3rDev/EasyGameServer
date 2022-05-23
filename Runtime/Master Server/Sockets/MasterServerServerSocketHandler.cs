using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using System.Timers;

/// <summary>
/// Class MasterServerServerSocketHandler, that controls the server listener socket.
/// </summary>
public class MasterServerServerSocketHandler : ServerSocketHandler
{
    #region Variables
    [Header("ID Assigner")]
    [Tooltip("Mutex that controls the concurrency to assign new users IDs")]
    private Mutex ID_mutex = new Mutex();

    [Tooltip("Integer that stores the next created user ID")]
    private int nextID = 0;


    [Header("Game Servers")]
    [Tooltip("Dictionary that stores the CURRENTLY CONNECTED game servers by their socket")]
    protected Dictionary<Socket, GameServerData> connectedGameServers = new Dictionary<Socket, GameServerData>();
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public MasterServerServerSocketHandler() : base()
    {

    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method HandleMessage, that receives a message from a client and do things based on it.
    /// </summary>
    /// <param name="content">Message content</param>
    /// <param name="handler">Socket that handles that connection</param>
    protected override void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        NetworkMessage receivedMessage = new NetworkMessage();
        try
        {
            receivedMessage = JsonUtility.FromJson<NetworkMessage>(content);
        }
        catch (Exception e)
        {
            Log.instance.WriteErrorLog("Error parsing receivedMessage from JSON: " + e.StackTrace, EasyGameServerControl.EnumLogDebugLevel.Minimal);
            throw e;
        }

        Log.instance.WriteLog("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
        " - Message type: " + receivedMessage.GetMessageType(), EasyGameServerControl.EnumLogDebugLevel.Complete);

        // Message to send back.
        NetworkMessage messageToSend = new NetworkMessage();

        // Local variables that are used in the cases below.
        string jsonMSG;
        UserData thisUser;
        UserData storedUser;
        int gameServerID;
        GameServerIPData gameServerIPData;
        long rttPing;
        string gameServerIP;
        int room;

        // Depending on the messageType, do different things.
        switch (receivedMessage.GetMessageType())
        {
            case MasterServerMessageTypes.RTT_RESPONSE_CLIENT:
                // Get the needed data.
                rttPing = roundTripTimes[handler].ReceiveRTT();
                thisUser = connectedUsers[handler];

                Log.instance.WriteLog("<color=#a52a2aff>Round Trip Time (Client):</color> " + thisUser.GetUsername() + " (" + rttPing + " ms).", EasyGameServerControl.EnumLogDebugLevel.Complete);

                // Call the onClientRTT delegate with UserID and the rtt ping in milliseconds.
                MasterServerDelegates.onClientRTT?.Invoke(thisUser.GetUserID(), rttPing);
                break;

            case MasterServerMessageTypes.RTT_RESPONSE_GAME_SERVER:
                // Get the needed data.
                rttPing = roundTripTimes[handler].ReceiveRTT();
                gameServerID = int.Parse(receivedMessage.GetMessageContent());

                Log.instance.WriteLog("<color=#a52a2aff>Round Trip Time (Game Server)</color> ID: " + receivedMessage.GetMessageContent() + " (" + rttPing + " ms).", EasyGameServerControl.EnumLogDebugLevel.Complete);

                // Call the onGameServerRTT delegate with GameServerID and the rtt ping in milliseconds.
                MasterServerDelegates.onGameServerRTT?.Invoke(gameServerID, rttPing);
                break;

            case MasterServerMessageTypes.USER_JOIN_SERVER:
                // Get the received user.
                thisUser = JsonUtility.FromJson<UserData>(receivedMessage.GetMessageContent());
                thisUser.SetSocket(handler);
                thisUser.SetIPAddress(handler.RemoteEndPoint.ToString());

                // Check if user is registered.
                if (allUsers.ContainsKey(thisUser.GetUserID()))
                {
                    // Update the saved user.
                    allUsers[thisUser.GetUserID()] = thisUser;
                }
                else
                {
                    // User has to be registered.
                    RegisterUser(thisUser);
                }

                // Put a heartbeat for the client socket.
                CreateRTT(handler, EasyGameServerControl.EnumInstanceType.Client);

                // Bool indicating if user comes from a game.
                bool returning = thisUser.GetRoom() != -1;

                if (returning)
                {
                    // Display data on the console.
                    Log.instance.WriteLog("<color=purple>Returning player from Game Server</color>: UserID: " + thisUser.GetUserID() + " - Username: " + thisUser.GetUsername() + " - IP: " + thisUser.GetIPAddress() + " - Room: " + thisUser.GetRoom() + ".", EasyGameServerControl.EnumLogDebugLevel.Minimal);

                    // Reset the user values.
                    thisUser.SetIngameID(-1);
                    thisUser.SetRoom(-1);
                    thisUser.SetLeftGame(false);

                    // Establish the message to RETURN.
                    messageToSend.SetMessageType(ClientMessageTypes.RETURN_TO_MASTER_SERVER);
                }
                else
                {
                    // Display data on the console.
                    Log.instance.WriteLog("<color=purple>Connected User</color>: UserID: " + thisUser.GetUserID() + " - Username: " + thisUser.GetUsername() + " - IP: " + thisUser.GetIPAddress() + ".", EasyGameServerControl.EnumLogDebugLevel.Minimal);

                    // Establish the message to JOIN.
                    messageToSend.SetMessageType(ClientMessageTypes.JOIN_MASTER_SERVER);
                }

                // Connect the user.
                ConnectUser(thisUser, handler);

                // Echo the data back to the client.
                messageToSend.SetMessageContent(JsonUtility.ToJson(thisUser));
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);

                // Call the onUserJoinServer delegate.
                MasterServerDelegates.onUserJoinServer?.Invoke(thisUser, returning);
                break;

            case MasterServerMessageTypes.DISCONNECT_USER:
                // Get the user.
                thisUser = connectedUsers[handler];

                // DisconnectFromMasterServer the user from the server.
                DisconnectUser(thisUser);

                // Echo the disconnection back to the client.
                messageToSend.SetMessageType(ClientMessageTypes.DISCONNECT);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);

                // Call the onUserDisconnect delegate.
                MasterServerDelegates.onUserDisconnect?.Invoke(thisUser);
                break;

            case MasterServerMessageTypes.QUEUE_JOIN:
                // Get the user.
                storedUser = connectedUsers[handler];

                // Get the received user.
                thisUser = JsonUtility.FromJson<UserData>(receivedMessage.GetMessageContent());

                // Update the base parameters.
                UpdateUserBaseParameters(thisUser, storedUser);

                // Add the player to the queue.
                ServerGamesManager.instance.EnqueueUser(thisUser);

                Log.instance.WriteLog("<color=#00ffffff>Searching game: </color>" + thisUser.GetUsername() + "<color=#00ffffff>.</color>", EasyGameServerControl.EnumLogDebugLevel.Extended);

                // Call the onUserJoinQueue delegate.
                MasterServerDelegates.onUserJoinQueue?.Invoke(thisUser);

                // Check the Queue to Start Game.
                ServerGamesManager.instance.CheckQueueToStartGame(this);
                break;

            case MasterServerMessageTypes.QUEUE_LEAVE:
                // Get the user.
                thisUser = connectedUsers[handler];

                // Bool to know if user is in queue.
                bool wasUserInQueue = ServerGamesManager.instance.LeaveFromQueue(thisUser);

                // If user was in queue.
                if (wasUserInQueue)
                {
                    // Log.
                    Log.instance.WriteLog("<color=#00ffffff>Leave Queue: </color>" + thisUser.GetUsername() + "<color=#00ffffff>.</color>", EasyGameServerControl.EnumLogDebugLevel.Extended);

                    // Call the onUserLeaveQueue delegate.
                    MasterServerDelegates.onUserLeaveQueue?.Invoke(thisUser);
                }
                break;

            case MasterServerMessageTypes.DISCONNECT_TO_GAME:
                // Get the user.
                thisUser = connectedUsers[handler];

                // DisconnectFromMasterServer the user from the server.
                DisconnectUserToGame(thisUser, handler);

                // Echo the disconnection back to the client.
                messageToSend.SetMessageType(ClientMessageTypes.DISCONNECT_TO_GAME);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);

                // Call the onUserDisconnectToGameServer delegate.
                MasterServerDelegates.onUserDisconnectToGameServer?.Invoke(thisUser);
                break;

            case MasterServerMessageTypes.USER_LEAVE_GAME:
                // Get the user from the GameServer.
                thisUser = JsonUtility.FromJson<UserData>(receivedMessage.GetMessageContent());

                Log.instance.WriteLog("<color=#00ffffff>User left game: </color>" + thisUser.GetUsername(), EasyGameServerControl.EnumLogDebugLevel.Minimal);

                // Assign the new user.
                allUsers[thisUser.GetUserID()] = thisUser;

                // Quit the user from the game.
                ServerGamesManager.instance.QuitUserFromGame(thisUser);

                // Call the onUserLeaveGame delegate.
                MasterServerDelegates.onUserLeaveGame?.Invoke(thisUser);
                break;

            case MasterServerMessageTypes.CREATED_GAME_SERVER:
                // Get the Game Server IP Data.
                gameServerIPData = JsonUtility.FromJson<GameServerIPData>(receivedMessage.GetMessageContent());

                // Get the gameServerID.
                gameServerID = gameServerIPData.GetGameServerID();

                // Add it to the dictionary of Game Servers connected.
                connectedGameServers.Add(handler, ServerGamesManager.instance.GetGameServerData(gameServerID));
                connectedGameServers[handler].SetIPAddress(handler.RemoteEndPoint.ToString());

                // Put a heartbeat for the client socket.
                CreateRTT(handler, EasyGameServerControl.EnumInstanceType.GameServer);

                // Get the game server IP from the message.
                gameServerIP = gameServerIPData.GetGameServerIP();

                // Update the GameServer status.
                ServerGamesManager.instance.UpdateGameServerStatus(gameServerID, GameServerData.EnumGameServerState.CREATED);

                Log.instance.WriteLog("<color=blue>Game Server created and connected</color>: [ID: " + gameServerID + " - IP: " + gameServerIP + "].", EasyGameServerControl.EnumLogDebugLevel.Useful);

                // Call the onGameServerCreated delegate.
                MasterServerDelegates.onGameServerCreated?.Invoke(gameServerID);

                // Send the game found data to the game server.
                string gameFoundDataJson = JsonUtility.ToJson(ServerGamesManager.instance.GetGameServerData(gameServerID).GetGameFoundData());

                messageToSend.SetMessageType(GameServerMessageTypes.RECEIVE_GAME_DATA);
                messageToSend.SetMessageContent(gameFoundDataJson);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);
                break;

            case MasterServerMessageTypes.READY_GAME_SERVER:
                // Get the Game Server IP Data.
                gameServerIPData = JsonUtility.FromJson<GameServerIPData>(receivedMessage.GetMessageContent());

                // Get the gameServerID.
                gameServerID = gameServerIPData.GetGameServerID();

                // Assign the created status.
                ServerGamesManager.instance.UpdateGameServerStatus(gameServerID, GameServerData.EnumGameServerState.WAITING_PLAYERS);

                // Get the game server IP from the message.
                gameServerIP = gameServerIPData.GetGameServerIP();

                // Call the onGameServerReady delegate.
                MasterServerDelegates.onGameServerReady?.Invoke(gameServerID);

                // Send the game server IP to the players.
                messageToSend.SetMessageType(ClientMessageTypes.CHANGE_TO_GAME_SERVER);
                messageToSend.SetMessageContent(gameServerIP);
                jsonMSG = messageToSend.ConvertMessage();

                room = ServerGamesManager.instance.GetGameServerData(gameServerID).GetRoom();

                foreach (UserData user in ServerGamesManager.instance.GetUsersInRooms()[room])
                {
                    Send(user.GetSocket(), jsonMSG);
                    Log.instance.WriteLog("<color=#00ffffff>Sent order to change to the game server to: </color>" + user.GetUsername() + "<color=#00ffffff>.</color>", EasyGameServerControl.EnumLogDebugLevel.Extended);
                }
                break;

            case MasterServerMessageTypes.GAME_START:
                // Get the Start Data.
                UpdateData startUpdateData = JsonUtility.FromJson<UpdateData>(receivedMessage.GetMessageContent());
                room = startUpdateData.GetRoom();

                Log.instance.WriteLog("<color=#00ffffff>Game Started on room: </color>" + room + "<color=#00ffffff>.</color>", EasyGameServerControl.EnumLogDebugLevel.Useful);

                // Update the GameServer status.
                gameServerID = connectedGameServers[handler].GetGameServerID();
                ServerGamesManager.instance.UpdateGameServerStatus(gameServerID, GameServerData.EnumGameServerState.STARTED_GAME);

                // Call the onGameStart delegate.
                MasterServerDelegates.onGameStart?.Invoke(startUpdateData);
                break;

            case MasterServerMessageTypes.GAME_END:
                // Get the Game End Information.
                GameEndData gameEndData = JsonUtility.FromJson<GameEndData>(receivedMessage.GetMessageContent());
                gameServerID = gameEndData.GetGameServerID();
                room = gameEndData.GetRoom();

                // Log the information.
                if (EasyGameServerConfig.DEBUG_MODE_CONSOLE >= EasyGameServerControl.EnumLogDebugLevel.Minimal)
                {
                    List<UserData> playersFromFinishedGame = ServerGamesManager.instance.GetUsersInRooms()[room];
                    List<int> playerIDsOrdered = gameEndData.GetPlayerIDsOrderList();

                    string playersString = "";
                    foreach (int playerID in playerIDsOrdered)
                    {
                        UserData iteratedUser = playersFromFinishedGame.Find(user => user.GetIngameID() == playerID);
                        playersString += (iteratedUser.GetUsername() + ", ");
                    }
                    
                    playersString = playersString.Substring(0, playersString.Length - 2) + ".";

                    Log.instance.WriteLog("<color=#00ffffff>Game finished for room </color>" + room + "<color=#00ffffff> on GameServer </color>" + gameServerID + "<color=#00ffffff>. Players: </color>" + playersString, EasyGameServerControl.EnumLogDebugLevel.Minimal);
                }

                // Update the GameServer status.
                ServerGamesManager.instance.UpdateGameServerStatus(gameServerID, GameServerData.EnumGameServerState.FINISHED);

                // Call the OnGameEndDelegate.
                MasterServerDelegates.onGameEnd?.Invoke(gameEndData);

                // Register all needed data and disconnect the Game Server.
                FinishAndDisconnectGameServer(room, gameServerID, handler);

                // Tell the Game Server to close.
                messageToSend.SetMessageType(GameServerMessageTypes.DISCONNECT_AND_CLOSE_GAMESERVER);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);
                break;

            case MasterServerMessageTypes.USER_DELETE:
                // Get the received user.
                thisUser = JsonUtility.FromJson<UserData>(receivedMessage.GetMessageContent());

                // Get the stored user.
                storedUser = connectedUsers[handler];

                // Update the base parameters.
                UpdateUserBaseParameters(thisUser, storedUser);

                // Delete the user.
                DeleteUser(thisUser);
                break;

            default:
                // Call the onMessageReceive delegate.
                MasterServerDelegates.onMessageReceive?.Invoke(receivedMessage);
                break;
        }
    }

    /// <summary>
    /// Method UpdateUserBaseParameters, to update the stored data with the received user.
    /// </summary>
    /// <param name="received">Received user data</param>
    /// <param name="stored">Stored user data</param>
    private void UpdateUserBaseParameters(UserData received, UserData stored)
    {
        received.SetUserID(stored.GetUserID());
        received.SetUsername(stored.GetUsername());
        received.SetSocket(stored.GetSocket());
        received.SetRoom(stored.GetRoom());
        received.SetIngameID(stored.GetIngameID());
    }

    #region Connect and disconnect methods
    /// <summary>
    /// Method OnNewConnection, that manages a new connection.
    /// </summary>
    /// <param name="client_socket">Socket connected to the client</param>
    protected override void OnNewConnection(Socket client_socket)
    {
        Log.instance.WriteLog("<color=#ff00ffff>New connection</color>. IP: " + client_socket.RemoteEndPoint + ".", EasyGameServerControl.EnumLogDebugLevel.Complete);

        // Ask client for user data.
        NetworkMessage msg = new NetworkMessage(ClientMessageTypes.CONNECT_TO_MASTER_SERVER, "");
        string jsonMSG = msg.ConvertMessage();

        Send(client_socket, jsonMSG);
    }

    /// <summary>
    /// Method OnClientDisconnected, that manages a disconnection.
    /// </summary>
    /// <param name="client_socket">Client socket disconnected from the server</param>
    /// <param name="clientType">Type of the client</param>
    public override void OnClientDisconnected(Socket client_socket, EasyGameServerControl.EnumInstanceType clientType)
    {
        string disconnectedIP = "";

        if (clientType.Equals(EasyGameServerControl.EnumInstanceType.Client))
        {
            UserData userToDisconnect = connectedUsers[client_socket];
            disconnectedIP = userToDisconnect.GetIPAddress();

            lock (connectedUsers)
            {
                connectedUsers.Remove(client_socket);
            }
        }
        else if (clientType.Equals(EasyGameServerControl.EnumInstanceType.GameServer))
        {
            GameServerData gameServer = connectedGameServers[client_socket];
            disconnectedIP = gameServer.GetIPAddress();

            lock (connectedGameServers)
            {
                connectedGameServers.Remove(client_socket);
            }
        }

        Log.instance.WriteLog("<color=#ff00ffff>Closed connection</color>. IP: " + disconnectedIP + ".", EasyGameServerControl.EnumLogDebugLevel.Complete);
    }
    #endregion

    #region User Management Methods
    /// <summary>
    /// Method DisconnectUserToGame, that disconnect an user's client so it can connect to the game server.
    /// </summary>
    /// <param name="userToDisconnect">User who disconnects for the game</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    private void DisconnectUserToGame(UserData userToDisconnect, Socket client_socket)
    {
        // DisconnectFromMasterServer the client.
        DisconnectClient(client_socket, EasyGameServerControl.EnumInstanceType.Client);

        // Display data on the console.
        Log.instance.WriteLog("<color=purple>Change connection to Game Server</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetIPAddress() + ".", EasyGameServerControl.EnumLogDebugLevel.Minimal);
    }

    /// <summary>
    /// Method ConnectUser, that connects an user to the server.
    /// </summary>
    /// <param name="userToConnect">User to connect to the server</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    protected override void ConnectUser(UserData userToConnect, Socket client_socket)
    {
        base.ConnectUser(userToConnect, client_socket);

        // Call the onUserConnect delegate.
        MasterServerDelegates.onUserConnect?.Invoke(userToConnect);
    }

    /// <summary>
    /// Method DisconnectUser, that disconnects an user from the server.
    /// </summary>
    /// <param name="userToDisconnect">User to disconnect from the server</param>
    protected override void DisconnectUser(UserData userToDisconnect)
    {
        base.DisconnectUser(userToDisconnect);

        // Display data on the console.
        Log.instance.WriteLog("<color=purple>Disconnected User</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetIPAddress() + ".", EasyGameServerControl.EnumLogDebugLevel.Minimal);

        // Call the onUserDisconnect delegate.
        MasterServerDelegates.onUserDisconnect?.Invoke(userToDisconnect);
    }

    /// <summary>
    /// Method DisconnectClientByTimeout, to disconnect a client when the timer was completed.
    /// </summary>
    /// <param name="sender">Object needed by the timer</param>
    /// <param name="e">ElapsedEventArgs needed by the timer</param>
    /// <param name="client_socket">Socket that handles the client</param>
    /// <param name="clientType">Type of the client</param>
    public override void DisconnectClientByTimeout(object sender, ElapsedEventArgs e, Socket client_socket, EasyGameServerControl.EnumInstanceType clientType)
    {
        if (clientType.Equals(EasyGameServerControl.EnumInstanceType.Client))
        {
            UserData userToDisconnect = connectedUsers[client_socket];
            Log.instance.WriteLog("<color=purple>Disconnected by timeout [CLIENT]</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetIPAddress() + ".", EasyGameServerControl.EnumLogDebugLevel.Minimal);
        }
        else if (clientType.Equals(EasyGameServerControl.EnumInstanceType.GameServer))
        {
            GameServerData gameServer = connectedGameServers[client_socket];
            Log.instance.WriteLog("<color=purple>Disconnected by timeout [GAME_SERVER]</color>: GameServerID: " + gameServer.GetGameServerID() + " - IP: " + gameServer.GetIPAddress() + ".", EasyGameServerControl.EnumLogDebugLevel.Minimal);
        }

        base.DisconnectClientByTimeout(sender, e, client_socket, clientType);
    }

    private void FinishAndDisconnectGameServer(int room, int gameServerID, Socket handler)
    {
        // Finish the Game and the GameServer.
        ServerGamesManager.instance.FinishedGame(room, gameServerID);

        // DisconnectFromMasterServer the Game Server.
        DisconnectClient(handler, EasyGameServerControl.EnumInstanceType.GameServer);
    }

    /// <summary>
    /// Method RegisterUser, that registers an user in the server.
    /// </summary>
    /// <param name="userToRegister"></param>
    private void RegisterUser(UserData userToRegister)
    {
        // Assign an user ID.
        try
        {
            ID_mutex.WaitOne();
            userToRegister.SetUserID(nextID);
            Interlocked.Increment(ref nextID);
        }
        catch (Exception e)
        {
            Log.instance.WriteErrorLog(e.ToString(), EasyGameServerControl.EnumLogDebugLevel.Minimal);
        }
        finally
        {
            ID_mutex.ReleaseMutex();
        }

        // Register it.
        lock (allUsers)
        {
            allUsers.Add(userToRegister.GetUserID(), userToRegister);
        }

        // Call the OnUserRegister delegate.
        MasterServerDelegates.onUserRegister?.Invoke(userToRegister);

        // Display data on the console.
        Log.instance.WriteLog("<color=purple>Registered User</color>: UserID: " + userToRegister.GetUserID() + " - Username: " + userToRegister.GetUsername() + ".", EasyGameServerControl.EnumLogDebugLevel.Minimal);
    }

    /// <summary>
    /// Method DeleteUser, that deletes an user from the server.
    /// </summary>
    /// <param name="userToDelete">User to be deleted from the server</param>
    private void DeleteUser(UserData userToDelete)
    {
        // Assing correct data to User.
        userToDelete.SetSocket(allUsers[userToDelete.GetUserID()].GetSocket());

        // Call the onUserDelete delegate.
        MasterServerDelegates.onUserDelete?.Invoke(userToDelete);

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
            allUsers.Remove(userToDelete.GetUserID());
        }

        // Send message to user.
        NetworkMessage msg = new NetworkMessage(ClientMessageTypes.USER_DELETE, JsonUtility.ToJson(userToDelete));
        Send(userToDelete.GetSocket(), msg.ConvertMessage());

        // Display data on the console.
        Log.instance.WriteLog("<color=purple>Deleted User</color>: UserID: " + userToDelete.GetUserID() + " - Username: " + userToDelete.GetUsername(), EasyGameServerControl.EnumLogDebugLevel.Minimal);
    }

    /// <summary>
    /// Method DisconnectAllUsers, that disconnects all the connected users to close the master server.
    /// </summary>
    public void DisconnectAllUsers()
    {
        UserData userToDisconnect;
        NetworkMessage disconnectMessage = new NetworkMessage(ClientMessageTypes.CLOSE_SERVER);
        string disconnectMessageJSON = disconnectMessage.ConvertMessage();
        
        // For each connected user, disconnect it.
        foreach (Socket clientSocket in connectedUsers.Keys)
        {
            userToDisconnect = connectedUsers[clientSocket];

            if (clientSocket.Connected)
            {
                // Send the client the message.
                Send(clientSocket, disconnectMessageJSON);

                // Close the socket and stop the Round Trip Time.
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();

                StopRTT(clientSocket);

                // Display data on the console.
                Log.instance.WriteLog("<color=purple>Disconnected User</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetIPAddress() + ".", EasyGameServerControl.EnumLogDebugLevel.Minimal);
            }
        }
    }
    #endregion
    #endregion
}