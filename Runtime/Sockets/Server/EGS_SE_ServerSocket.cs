using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Linq;
using System.Timers;

/// <summary>
/// Class EGS_SE_ServerSocket, that controls the server listener socket.
/// </summary>
public class EGS_SE_ServerSocket : EGS_ServerSocket
{
    #region Variables
    [Header("ID Assigner")]
    [Tooltip("Mutex that controls the concurrency to assign new users IDs")]
    private Mutex IDMutex = new Mutex();
    [Tooltip("Integer that stores the next created user ID")]
    private int nextID = 0;


    [Header("Game Servers")]
    [Tooltip("Dictionary that stores the CURRENTLY CONNECTED game servers by their socket")]
    protected Dictionary<Socket, EGS_GameServerData> connectedGameServers = new Dictionary<Socket, EGS_GameServerData>();

    [Header("References")]
    [Tooltip("Reference to the Log")]
    private EGS_Log egs_Log = null;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    /// <param name="log">Reference to the log</param>
    public EGS_SE_ServerSocket(EGS_Log log) : base()
    {
        this.egs_Log = log;
    }
    #endregion

    #region Class Methods
    #region Private Methods
    /// <summary>
    /// Method HandleMessage, that receives a message from a client and do things based on it.
    /// </summary>
    /// <param name="content">Message content</param>
    /// <param name="handler">Socket that handles that connection</param>
    protected override void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        EGS_Message receivedMessage = new EGS_Message();
        try
        {
            receivedMessage = JsonUtility.FromJson<EGS_Message>(content);
        }
        catch (Exception e)
        {
            egs_Log.LogError("Error parsing receivedMessage from JSON: " + e.StackTrace);
            throw e;
        }

        if (EGS_Config.DEBUG_MODE > 2)
            egs_Log.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);

        // Message to send back.
        EGS_Message messageToSend = new EGS_Message();

        // Local variables that are used in the cases below.
        string jsonMSG;
        EGS_User thisUser;
        int gameServerID;
        long rttPing;
        string gameServerIP;
        string[] messageInfo;

        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "RTT_RESPONSE_CLIENT":
                // Get the needed data.
                rttPing = roundTripTimes[handler].ReceiveRTT();
                thisUser = connectedUsers[handler];

                if (EGS_Config.DEBUG_MODE > 2)
                    egs_Log.Log("<color=blue>Round Trip Time (Client):</color> " + thisUser.GetUsername() + " (" + rttPing + " ms).");

                // Call the onClientRTT delegate with UserID and the rtt ping in milliseconds.
                EGS_MasterServerDelegates.onClientRTT?.Invoke(thisUser.GetUserID(), rttPing);
                break;
            case "RTT_RESPONSE_GAME_SERVER":
                // Get the needed data.
                rttPing = roundTripTimes[handler].ReceiveRTT();
                gameServerID = int.Parse(receivedMessage.messageContent);

                if (EGS_Config.DEBUG_MODE > 2)
                    egs_Log.Log("<color=blue>Round Trip Time (Game Server)</color> ID: " + receivedMessage.messageContent + " (" + rttPing + " ms).");

                // Call the onGameServerRTT delegate with GameServerID and the rtt ping in milliseconds.
                EGS_MasterServerDelegates.onGameServerRTT?.Invoke(gameServerID, rttPing);
                break;
            case "USER_JOIN_SERVER":
                // Get the received user.
                thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
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
                CreateRTT(handler, EGS_Control.EGS_Type.Client);

                // Bool indicating if user comes from a game.
                bool returning = thisUser.GetRoom() != -1;

                if (returning)
                {
                    // Display data on the console.
                    if (EGS_Config.DEBUG_MODE > -1 && !thisUser.DidLeaveGame())
                        egs_Log.Log("<color=purple>Returning player from Game Server</color>: UserID: " + thisUser.GetUserID() + " - Username: " + thisUser.GetUsername() + " - IP: " + thisUser.GetIPAddress() + " - Room: " + thisUser.GetRoom() + ".");

                    // Reset the user values.
                    thisUser.SetIngameID(-1);
                    thisUser.SetRoom(-1);

                    // Establish the message to RETURN.
                    messageToSend.messageType = "RETURN_TO_MASTER_SERVER";
                }
                else
                {
                    // Establish the message to JOIN.
                    messageToSend.messageType = "JOIN_MASTER_SERVER";
                }

                // Check if the user left its last game.
                if (thisUser.DidLeaveGame())
                {
                    // Reset the value.
                    thisUser.SetLeftGame(false);

                    // Display data on the console.
                    if (EGS_Config.DEBUG_MODE > -1)
                        egs_Log.Log("<color=purple>Player left game</color>: UserID: " + thisUser.GetUserID() + " - Username: " + thisUser.GetUsername() + " - IP: " + thisUser.GetIPAddress() + " - Room: " + thisUser.GetRoom() + ".");
                }

                // Connect the user.
                ConnectUser(thisUser, handler);

                // Echo the data back to the client.
                messageToSend.messageContent = JsonUtility.ToJson(thisUser);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);

                // Call the onUserJoinServer delegate.
                EGS_MasterServerDelegates.onUserJoinServer?.Invoke(thisUser, returning);
                break;
            case "DISCONNECT_USER":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Disconnect the user from the server.
                DisconnectUser(thisUser);

                // Echo the disconnection back to the client.
                messageToSend.messageType = "DISCONNECT";
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);

                // Call the onUserDisconnect delegate.
                EGS_MasterServerDelegates.onUserDisconnect?.Invoke(thisUser);
                break;
            case "QUEUE_JOIN":
                // Get the user.
                EGS_User storedUser = connectedUsers[handler];

                // Get the received user.
                thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);

                // Update the base parameters.
                UpdateUserBaseParameters(thisUser, storedUser);

                // Add the player to the queue.
                EGS_ServerGamesManager.instance.searchingGame_Users.Enqueue(thisUser); // TODO: Encapsulate.

                if (EGS_Config.DEBUG_MODE > 2)
                    egs_Log.Log("Searching game: " + thisUser.GetUsername() + ". User Data: " + receivedMessage.messageContent);
                else if (EGS_Config.DEBUG_MODE > 0)
                    egs_Log.Log("Searching game: " + thisUser.GetUsername() + ".");

                // Call the onUserJoinQueue delegate.
                EGS_MasterServerDelegates.onUserJoinQueue?.Invoke(thisUser);

                // Check the Queue to Start Game.
                EGS_ServerGamesManager.instance.CheckQueueToStartGame(this);
                break;
            case "QUEUE_LEAVE":
                // Bool to know if player is in queue.
                bool isUserInQueue = false;

                // Get the user.
                thisUser = connectedUsers[handler];

                // Lock the queue. // TODO: Encapsulate.
                lock (EGS_ServerGamesManager.instance.searchingGame_Users)
                {
                    // Check if user is in queue.
                    foreach (EGS_User userInQueue in EGS_ServerGamesManager.instance.searchingGame_Users)
                    {
                        if (userInQueue.GetUserID() == thisUser.GetUserID())
                        {
                            isUserInQueue = true;

                            // Remove the player from the Queue by constructing a new queue based on the previous one but without the left player.
                            EGS_ServerGamesManager.instance.searchingGame_Users =
                                new ConcurrentQueue<EGS_User>(EGS_ServerGamesManager.instance.searchingGame_Users.Where(x => x.GetSocket() != handler));

                            break;
                        }
                    }
                }

                if (isUserInQueue)
                {
                    if (EGS_Config.DEBUG_MODE > 0)
                        egs_Log.Log("Leave Queue: " + thisUser.GetUsername() + ".");

                    // Call the onUserLeaveQueue delegate.
                    EGS_MasterServerDelegates.onUserLeaveQueue?.Invoke(thisUser);
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

                // Call the onUserDisconnectToGameServer delegate.
                EGS_MasterServerDelegates.onUserDisconnectToGameServer?.Invoke(thisUser);
                break;
            case "USER_LEAVE_GAME":
                // Get the user from the GameServer.
                thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);

                egs_Log.Log("User left: " + thisUser.GetUsername());

                // Assign the new user.
                allUsers[thisUser.GetUserID()] = thisUser;

                // TODO: Check if was leave closing or leave to the master server.

                // Quit the user from the game.
                EGS_ServerGamesManager.instance.QuitUserFromGame(thisUser);

                // Call the onUserLeaveGame delegate.
                EGS_MasterServerDelegates.onUserLeaveGame?.Invoke(thisUser);
                break;
            case "CREATED_GAME_SERVER":
                // Get the message info.
                messageInfo = receivedMessage.messageContent.Split('#');

                // Get the gameServerID.
                gameServerID = int.Parse(messageInfo[0]);

                // Add it to the dictionary of Game Servers connected.
                connectedGameServers.Add(handler, EGS_ServerGamesManager.instance.gameServers[gameServerID]);
                connectedGameServers[handler].SetIPAddress(handler.RemoteEndPoint.ToString());

                // Put a heartbeat for the client socket.
                CreateRTT(handler, EGS_Control.EGS_Type.GameServer);

                // Get the game server IP from the message.
                gameServerIP = messageInfo[1];

                if (EGS_Config.DEBUG_MODE > -1)
                    egs_Log.Log("<color=purple>Game Server created and connected</color>: [ID: " + gameServerID + " - IP: " + gameServerIP + "].");

                // Assign the created status. // TODO: Encapsulate.
                EGS_ServerGamesManager.instance.gameServers[gameServerID].SetStatus(EGS_GameServerData.EGS_GameServerState.WAITING_PLAYERS);

                // Call the onGameServerCreated delegate.
                EGS_MasterServerDelegates.onGameServerCreated?.Invoke(gameServerID);

                // Send the game found data to the game server.
                string gameFoundDataJson = JsonUtility.ToJson(EGS_ServerGamesManager.instance.gameServers[gameServerID].GetGameFoundData());

                messageToSend.messageType = "RECEIVE_GAME_DATA";
                messageToSend.messageContent = gameFoundDataJson;
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);
                break;
            case "READY_GAME_SERVER":
                // Get the message info.
                messageInfo = receivedMessage.messageContent.Split('#');

                // Get the gameServerID.
                gameServerID = int.Parse(messageInfo[0]);

                // Assign the created status. // TODO: Encapsulate.
                EGS_ServerGamesManager.instance.gameServers[gameServerID].SetStatus(EGS_GameServerData.EGS_GameServerState.WAITING_PLAYERS);

                // Get the game server IP from the message.
                gameServerIP = messageInfo[1];

                // Call the onGameServerReady delegate.
                EGS_MasterServerDelegates.onGameServerReady?.Invoke(gameServerID);

                // Send the game server IP to the players.
                messageToSend.messageType = "CHANGE_TO_GAME_SERVER";
                messageToSend.messageContent = gameServerIP;
                jsonMSG = messageToSend.ConvertMessage();

                int roomID = EGS_ServerGamesManager.instance.gameServers[gameServerID].GetRoom();

                foreach (EGS_User user in EGS_ServerGamesManager.instance.usersInRooms[roomID])
                {
                    Send(user.GetSocket(), jsonMSG);

                    if (EGS_Config.DEBUG_MODE > 1)
                        egs_Log.Log("SENT ORDER TO: " + user.GetUsername() + ".");
                }
                break;
            case "GAME_END":
                // TODO: Save the information and tell the Game Server to close.
                // Get the Game End Information.
                EGS_GameEndData gameEndData = JsonUtility.FromJson<EGS_GameEndData>(receivedMessage.messageContent);
                gameServerID = gameEndData.GetGameServerID();
                roomID = gameEndData.GetRoom();

                // Log the information.
                if (EGS_Config.DEBUG_MODE > 0)
                {
                    List<EGS_User> playersFromFinishedGame = EGS_ServerGamesManager.instance.usersInRooms[roomID];
                    List<int> playerIDsOrdered = gameEndData.GetPlayerIDsOrderList();

                    string playersString = "";
                    foreach (int playerID in playerIDsOrdered)
                    {
                        EGS_User iteratedUser = playersFromFinishedGame.Find(user => user.GetIngameID() == playerID);
                        playersString += (iteratedUser.GetUsername() + ", ");
                    }
                    
                    playersString = playersString.Substring(0, playersString.Length - 2) + ".";

                    egs_Log.Log("Game finished for room " + roomID + " on GameServer " + gameServerID + ". Players: " + playersString);
                }

                // Update the GameServer status.
                EGS_ServerGamesManager.instance.gameServers[gameServerID].SetStatus(EGS_GameServerData.EGS_GameServerState.FINISHED);

                // Call the OnGameEndDelegate.
                EGS_MasterServerDelegates.onGameEnd?.Invoke(gameEndData);

                // Register all needed data and disconnect the Game Server.
                FinishAndDisconnectGameServer(roomID, gameServerID, handler);

                // Tell the Game Server to close.
                messageToSend.messageType = "DISCONNECT_AND_CLOSE_GAMESERVER";
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);
                break;
            default:
                // Call the onMessageReceive delegate.
                EGS_MasterServerDelegates.onMessageReceive?.Invoke(receivedMessage);
                break;
        }
    }

    private void UpdateUserBaseParameters(EGS_User received, EGS_User stored)
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
        if (EGS_Config.DEBUG_MODE > 2)
            egs_Log.Log("<color=blue>New connection</color>. IP: " + client_socket.RemoteEndPoint + ".");

        // Ask client for user data.
        EGS_Message msg = new EGS_Message();
        msg.messageType = "CONNECT_TO_MASTER_SERVER";
        string jsonMSG = msg.ConvertMessage();

        Send(client_socket, jsonMSG);
    }

    /// <summary>
    /// Method OnClientDisconnected, that manages a disconnection.
    /// </summary>
    /// <param name="client_socket">Client socket disconnected from the server</param>
    /// <param name="clientType">Type of the client</param>
    public override void OnClientDisconnected(Socket client_socket, EGS_Control.EGS_Type clientType)
    {
        string disconnectedIP = "";

        if (clientType.Equals(EGS_Control.EGS_Type.Client))
        {
            EGS_User userToDisconnect = connectedUsers[client_socket];
            disconnectedIP = userToDisconnect.GetIPAddress();

            lock (connectedUsers)
            {
                connectedUsers.Remove(client_socket);
            }
        }
        else if (clientType.Equals(EGS_Control.EGS_Type.GameServer))
        {
            EGS_GameServerData gameServer = connectedGameServers[client_socket];
            disconnectedIP = gameServer.GetIPAddress();

            lock (connectedGameServers)
            {
                connectedGameServers.Remove(client_socket);
            }
        }

        if (EGS_Config.DEBUG_MODE > 2)
            egs_Log.Log("<color=blue>Closed connection</color>. IP: " + disconnectedIP + ".");
    }
    #endregion

    #region User Management Methods
    /// <summary>
    /// Method DisconnectUserToGame, that disconnect an user's client so it can connect to the game server.
    /// </summary>
    /// <param name="userToDisconnect">User who disconnects for the game</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    private void DisconnectUserToGame(EGS_User userToDisconnect, Socket client_socket)
    {
        // Disconnect the client.
        DisconnectClient(client_socket, EGS_Control.EGS_Type.Client);

        // Display data on the console.
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Disconnected To Connect to the Game Server</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetIPAddress() + ".");
    }

    /// <summary>
    /// Method ConnectUser, that connects an user to the server.
    /// </summary>
    /// <param name="userToConnect">User to connect to the server</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    protected override void ConnectUser(EGS_User userToConnect, Socket client_socket)
    {
        base.ConnectUser(userToConnect, client_socket);

        // Display data on the console.
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Connected User</color>: UserID: " + userToConnect.GetUserID() + " - Username: " + userToConnect.GetUsername() + " - IP: " + userToConnect.GetIPAddress() + ".");

        // Call the onUserConnect delegate.
        EGS_MasterServerDelegates.onUserConnect?.Invoke(userToConnect);
    }

    /// <summary>
    /// Method DisconnectUser, that disconnects an user from the server.
    /// </summary>
    /// <param name="userToDisconnect">User to disconnect from the server</param>
    protected override void DisconnectUser(EGS_User userToDisconnect)
    {
        base.DisconnectUser(userToDisconnect);

        // Display data on the console.
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Disconnected User</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetIPAddress() + ".");

        // Call the onUserDisconnect delegate.
        EGS_MasterServerDelegates.onUserDisconnect?.Invoke(userToDisconnect);
    }

    /// <summary>
    /// Method DisconnectClientByTimeout, to disconnect a client when the timer was completed.
    /// </summary>
    /// <param name="sender">Object needed by the timer</param>
    /// <param name="e">ElapsedEventArgs needed by the timer</param>
    /// <param name="client_socket">Socket that handles the client</param>
    /// <param name="clientType">Type of the client</param>
    public override void DisconnectClientByTimeout(object sender, ElapsedEventArgs e, Socket client_socket, EGS_Control.EGS_Type clientType)
    {
        if (clientType.Equals(EGS_Control.EGS_Type.Client))
        {
            EGS_User userToDisconnect = connectedUsers[client_socket];
            if (EGS_Config.DEBUG_MODE > -1)
                egs_Log.Log("<color=blue>Disconnected by timeout [CLIENT]</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetIPAddress() + ".");
        }
        else if (clientType.Equals(EGS_Control.EGS_Type.GameServer))
        {
            EGS_GameServerData gameServer = connectedGameServers[client_socket];
            if (EGS_Config.DEBUG_MODE > -1)
                egs_Log.Log("<color=blue>Disconnected by timeout [GAME_SERVER]</color>: GameServerID: " + gameServer.GetGameServerID() + " - IP: " + gameServer.GetIPAddress() + ".");
        }

        base.DisconnectClientByTimeout(sender, e, client_socket, clientType);
    }

    private void FinishAndDisconnectGameServer(int room, int gameServerID, Socket handler)
    {
        // Finish the Game and the GameServer.
        EGS_ServerGamesManager.instance.FinishedGame(room, gameServerID, handler);

        // Disconnect the Game Server.
        DisconnectClient(handler, EGS_Control.EGS_Type.GameServer);
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
            if (EGS_Config.DEBUG_MODE > -1)
                egs_Log.LogError(e.ToString());
        }
        finally
        {
            IDMutex.ReleaseMutex();
        }

        // Register it.
        lock (allUsers)
        {
            allUsers.Add(userToRegister.GetUserID(), userToRegister);
        }

        // Call the OnUserRegister delegate.
        EGS_MasterServerDelegates.onUserRegister?.Invoke(userToRegister);

        // Display data on the console.
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Registered User</color>: UserID: " + userToRegister.GetUserID() + " - Username: " + userToRegister.GetUsername() + ".");
    }

    /// <summary>
    /// Method DeleteUser, that deletes an user from the server.
    /// </summary>
    /// <param name="userToDelete">User to be deleted from the server</param>
    private void DeleteUser(EGS_User userToDelete)
    {
        // Assing correct data to User.
        userToDelete.SetSocket(allUsers[userToDelete.GetUserID()].GetSocket());

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
        EGS_Message msg = new EGS_Message();
        msg.messageType = "DELETE_USER";
        msg.messageContent = JsonUtility.ToJson(userToDelete);

        Send(userToDelete.GetSocket(), msg.ConvertMessage());

        // Display data on the console.
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Deleted User</color>: UserID: " + userToDelete.GetUserID() + " - Username: " + userToDelete.GetUsername());

        // Call the onUserDelete delegate.
        EGS_MasterServerDelegates.onUserDelete?.Invoke(userToDelete);
    }
    #endregion
    #endregion
    #endregion
}