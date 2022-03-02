using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Linq;

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
    /// <param name="log">Reference to the log</param>
    /// <param name="onNewConnection">Delegate to execute on a new connection</param>
    /// <param name="onClientDisconnect">Delegate to execute on a client disconnection</param>
    public EGS_SE_ServerSocket(EGS_Log log, Action<Socket> onNewConnection, Action<Socket> onClientDisconnect) : base(onNewConnection, onClientDisconnect)
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

        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "RTT_RESPONSE_CLIENT":
                rttPing = roundTripTimes[handler].ReceiveRTT();

                if (EGS_Config.DEBUG_MODE > 2)
                    egs_Log.Log("<color=blue>Round Trip Time:</color> " + connectedUsers[handler].GetUsername() + " (" + rttPing + " ms).");
                break;
            case "RTT_RESPONSE_GAME_SERVER":
                rttPing = roundTripTimes[handler].ReceiveRTT();

                if (EGS_Config.DEBUG_MODE > 2)
                    egs_Log.Log("<color=blue>Round Trip Time (Game Server)</color> ID: " + receivedMessage.messageContent + " (" + rttPing + " ms).");
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
                CreateRTT(handler);

                // Echo the data back to the client.
                messageToSend.messageType = "JOIN_MASTER_SERVER";
                messageToSend.messageContent = JsonUtility.ToJson(thisUser);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);
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
                break;
            case "QUEUE_JOIN":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Add the player to the queue.
                EGS_ServerGamesManager.gm_instance.searchingGame_Users.Enqueue(thisUser); // TODO: Encapsulate.

                if (EGS_Config.DEBUG_MODE > 0)
                    egs_Log.Log("Searching game: " + thisUser.GetUsername() + ".");

                // Check the Queue to Start Game.
                EGS_ServerGamesManager.gm_instance.CheckQueueToStartGame(this);
                break;
            case "QUEUE_LEAVE":
                // Bool to know if player is in queue.
                bool isUserInQueue = false;

                // Get the user.
                thisUser = connectedUsers[handler];

                // Lock the queue. // TODO: Encapsulate.
                lock (EGS_ServerGamesManager.gm_instance.searchingGame_Users)
                {
                    // Check if user is in queue.
                    foreach (EGS_User userInQueue in EGS_ServerGamesManager.gm_instance.searchingGame_Users)
                    {
                        if (userInQueue.GetUserID() == thisUser.GetUserID())
                        {
                            isUserInQueue = true;
                            break;
                        }
                    }

                    if (isUserInQueue)
                    {
                        // Remove the player from the Queue by constructing a new queue based on the previous one but without the left player.
                        EGS_ServerGamesManager.gm_instance.searchingGame_Users =
                            new ConcurrentQueue<EGS_User>(EGS_ServerGamesManager.gm_instance.searchingGame_Users.Where(x => x.GetSocket() != handler));

                        if (EGS_Config.DEBUG_MODE > 0)
                            egs_Log.Log("Leave Queue: " + thisUser.GetUsername() + ".");
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
                CreateRTT(handler);

                // Get the game server IP from the message.
                // TODO: Maybe get it from the handler.
                string gameServerIP = messageInfo[1];

                if (EGS_Config.DEBUG_MODE > -1)
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

                    if (EGS_Config.DEBUG_MODE > 1)
                        egs_Log.Log("SENT ORDER TO: " + user.GetUsername() + ".");
                }
                break;
            default:
                if (EGS_Config.DEBUG_MODE > -1)
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
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Disconnected To Connect to the Game Server</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");
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
            egs_Log.Log("<color=purple>Connected User</color>: UserID: " + userToConnect.GetUserID() + " - Username: " + userToConnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");
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
            egs_Log.Log("<color=purple>Disconnected User</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetSocket().RemoteEndPoint + ".");
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
            allUsers.Add(userToRegister.GetUsername(), userToRegister);
        }

        // TODO: Save on a database? Delegate.

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
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("<color=purple>Deleted User</color>: UserID: " + userToDelete.GetUserID() + " - Username: " + userToDelete.GetUsername());
    }
    #endregion
    #endregion
    #endregion
}