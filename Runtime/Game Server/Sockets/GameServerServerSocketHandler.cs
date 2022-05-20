using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class GameServerServerSocketHandler, that controls the server receiver socket.
/// </summary>
public class GameServerServerSocketHandler : ServerSocketHandler
{
    #region Variables
    [Header("References")]
    [Tooltip("Reference to the socket manager")]
    private GameServerSocketManager socketManager;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public GameServerServerSocketHandler(GameServerSocketManager socketManager) : base()
    {
        this.socketManager = socketManager;

        // Get the info of users to this game.
        foreach (UserData userToGame in GameServer.instance.GetGameFoundData().GetUsersToGame())
        {
            this.allUsers.Add(userToGame.GetUserID(), userToGame);
        }
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, that opens the socket to connections.
    /// </summary>
    /// <param name="localEP">EndPoint where the GameServer will listen</param>
    /// <param name="socket_listener">Socket to use</param>
    /// <param name="connections">Number of connections to listen</param>
    public override void StartListening(EndPoint localEP, Socket socket_listener, int connections)
    {
        base.StartListening(localEP, socket_listener, connections);
        this.socketManager.GetStartDoneMRE().Set();
    }

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
            // Log.
            MainThreadDispatcher.RunOnMainThread(()=> { GameServer.instance.console_text.text += ("\nPError parsing receivedMessage from JSON: " + e.StackTrace); });
            throw e;
        }

        // Message to send back.
        NetworkMessage messageToSend = new NetworkMessage();

        // Local variables that are used in the cases below.
        string jsonMSG;
        UserData thisUser;
        NetworkPlayer thisPlayer;
        long rttPing;
        string leavingGameString;

        // Depending on the messageType, do different things.
        switch (receivedMessage.GetMessageType())
        {
            case "RTT_RESPONSE_CLIENT":
                // Get the needed data.
                rttPing = roundTripTimes[handler].ReceiveRTT();
                thisUser = connectedUsers[handler];

                // TODO: Log in the Game Server Console.
                /*if (EasyGameServerConfig.DEBUG_MODE_CONSOLE > EasyGameServerControl.EnumLogDebugLevel.FullWithEveryMessage)
                    Log.instance.Log("<color=blue>Round Trip Time (Client):</color> " + thisUser.GetUsername() + " (" + rttPing + " ms).");*/

                // Call the onReceiveClientRTT delegate with UserID and the rtt ping in milliseconds.
                GameServerDelegates.onReceiveClientRTT?.Invoke(thisUser.GetUserID(), rttPing);

                // TODO: Update UI? I think it is better on UI Update method or in the delegate.
                break;
            case "JOIN_GAME_SERVER":
                try
                {
                    // Get the received user
                    thisUser = JsonUtility.FromJson<UserData>(receivedMessage.GetMessageContent());
                    thisUser.SetSocket(handler);
                    thisUser.SetIPAddress(handler.RemoteEndPoint.ToString());

                    // If the user is on the list to play this game.
                    if (allUsers.ContainsKey(thisUser.GetUserID()))
                    {
                        // Log.
                        MainThreadDispatcher.RunOnMainThread(() => { GameServer.instance.console_text.text += "\nPLAYER JOINED: " + thisUser.GetUsername(); });
                            
                        // Connect the user.
                        ConnectUser(thisUser, handler);

                        // Put a heartbeat for the client socket.
                        CreateRTT(handler, EasyGameServerControl.EnumInstanceType.Client);

                        // Echo the data back to the client.
                        messageToSend.SetMessageType("JOIN_GAME_SERVER");
                        jsonMSG = messageToSend.ConvertMessage();
                        Send(handler, jsonMSG);

                        // Call the onUserJoinServer delegate.
                        GameServerDelegates.onUserJoinServer?.Invoke(thisUser);

                        // Check if game started / are all players.
                        // TODO: Only prepare the game, not start it.
                        bool allPlayersConnected = GameServer.instance.GetGame().Ready();
                        // Log.
                        MainThreadDispatcher.RunOnMainThread(() => { GameServer.instance.console_text.text += "\nAllPlayersConnected: " + allPlayersConnected; });

                        if (allPlayersConnected)
                        {
                            // Call the onAllPlayersConnected delegate.
                            GameServerDelegates.onAllPlayersConnected?.Invoke();

                            // Load the Game Scene.
                            LoadScene(GameServer.instance.GetGame().GetGameSceneName());
                        }
                    }
                }
                catch (Exception e)
                {
                    // Log.
                    MainThreadDispatcher.RunOnMainThread(() => { GameServer.instance.console_text.text += "\nEXCEPTION: " + e.ToString(); });
                }
                break;
            case "PLAYER_INPUT":
                // Get the input data
                PlayerInputs playerInputs = JsonUtility.FromJson<PlayerInputs>(receivedMessage.GetMessageContent());
                bool[] inputs = playerInputs.GetInputs();

                // Get the player from its ingameID.
                thisPlayer = NetworkGameManager.instance.GetPlayerByID(playerInputs.GetIngameID());

                // Assign its inputs.
                thisPlayer.SetInputs(inputs);

                // Call the onPlayerSendInput delegate.
                GameServerDelegates.onPlayerSendInput?.Invoke(thisPlayer, playerInputs);
                break;
            case "LEAVE_GAME":
                // Get the user.
                thisUser = connectedUsers[handler];

                // Get the player.
                int playerID = int.Parse(receivedMessage.GetMessageContent());
                thisPlayer = NetworkGameManager.instance.GetPlayerByID(playerID);

                // Remove the player from the game.
                GameServer.instance.GetGame().QuitPlayerFromGame(thisPlayer);

                // DisconnectFromMasterServer the user from the server.
                DisconnectUser(thisUser);

                // Echo the disconnection back to the client.
                leavingGameString = bool.TrueString;

                messageToSend.SetMessageType("DISCONNECT_TO_MASTER_SERVER");
                messageToSend.SetMessageContent(leavingGameString);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);

                // Call the onPlayerLeaveGame delegate.
                GameServerDelegates.onPlayerLeaveGame?.Invoke(thisPlayer);
                break;
            case "RETURN_TO_MASTER_SERVER":
                // Get the user.
                thisUser = connectedUsers[handler];

                // DisconnectUserToMasterServer the user from the server.
                DisconnectUserToMasterServer(thisUser, handler);

                // Echo the disconnection back to the client.
                leavingGameString = bool.FalseString;

                messageToSend.SetMessageType("DISCONNECT_TO_MASTER_SERVER");
                messageToSend.SetMessageContent(leavingGameString);
                jsonMSG = messageToSend.ConvertMessage();

                Send(handler, jsonMSG);

                // Call the onUserDisconnectToMasterServer delegate.
                GameServerDelegates.onUserDisconnectToMasterServer?.Invoke(thisUser);
                break;
            default:
                // Call the onClientMessageReceive delegate.
                GameServerDelegates.onClientMessageReceive?.Invoke(receivedMessage, this, handler);
                break;
        }
    }

    #region Connect and disconnect methods
    /// <summary>
    /// Method OnNewConnection, that manages a new connection.
    /// </summary>
    /// <param name="client_socket">Socket connected to the client</param>
    protected override void OnNewConnection(Socket client_socket)
    {
        // Log.
        MainThreadDispatcher.RunOnMainThread(() => { GameServer.instance.console_text.text += "\nPLAYER CONNECTED: " + client_socket.RemoteEndPoint; });

        // Ask client for user data.
        NetworkMessage msg = new NetworkMessage("CONNECT_TO_GAME_SERVER", "");
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

        // LOG.
        /*if (EasyGameServerConfig.DEBUG_MODE_CONSOLE > 2)
            Log.instance.Log("<color=blue>Closed connection</color>. IP: " + disconnectedIP + ".");*/
    }
    #endregion

    #region User Management Methods
    /// <summary>
    /// Method DisconnectUserToMasterServer, that disconnect an user's client so it can connect to the master server.
    /// </summary>
    /// <param name="userToDisconnect">User who disconnects for the game</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    private void DisconnectUserToMasterServer(UserData userToDisconnect, Socket client_socket)
    {
        // DisconnectFromMasterServer the client.
        DisconnectClient(client_socket, EasyGameServerControl.EnumInstanceType.Client);

        // Update the players still connected value for the end controller.
        MainThreadDispatcher.RunOnMainThread(() => { GameServerEndController.instance.UpdateNumOfPlayersConnected(socketManager, userToDisconnect); });

        // TODO: Log working.
        /*// Display data on the console.
        if (EasyGameServerConfig.DEBUG_MODE_CONSOLE > -1)
            Log.instance.Log("<color=purple>Disconnected To Connect to the Game Server</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");*/
    }

    /// <summary>
    /// Method ConnectUser, that connects an user to the server.
    /// </summary>
    /// <param name="userToConnect">User to connect to the server</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    protected override void ConnectUser(UserData userToConnect, Socket client_socket)
    {
        base.ConnectUser(userToConnect, client_socket);

        // Display data on the console. // LOG.
        //if (EasyGameServerConfig.DEBUG_MODE_CONSOLE > -1)
            //Log.instance.Log("<color=purple>Connected User</color>: UserID: " + userToConnect.GetUserID() + " - Username: " + userToConnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");

        // Call the onUserConnect delegate.
        GameServerDelegates.onUserConnect?.Invoke(userToConnect);
    }

    /// <summary>
    /// Method DisconnectUser, that disconnects an user from the server.
    /// </summary>
    /// <param name="userToDisconnect">User to disconnect from the server</param>
    protected override void DisconnectUser(UserData userToDisconnect)
    {
        base.DisconnectUser(userToDisconnect);

        // Update the players still connected value for the end controller.
        MainThreadDispatcher.RunOnMainThread(() => { GameServerEndController.instance.UpdateNumOfPlayersConnected(socketManager, userToDisconnect); });

        // Display data on the console. // LOG.
        //if (EasyGameServerConfig.DEBUG_MODE_CONSOLE > -1)
        //Log.instance.Log("<color=purple>Disconnected User</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetSocket().RemoteEndPoint + ".");

        // Call the onUserDisconnect delegate.
        GameServerDelegates.onUserDisconnect?.Invoke(userToDisconnect);
    }

    /// <summary>
    /// Method DisconnectUserBySocketException, called when GameServer can't message a client.
    /// </summary>
    /// <param name="userToDisconnect">User to disconnect</param>
    public void DisconnectUserBySocketException(UserData userToDisconnect)
    {
        DisconnectUser(userToDisconnect);
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
        // Need this function to be empty since the client will be disconnected due to a sending error.
    }
    #endregion

    #region Main Thread Functions
    /// <summary>
    /// Method LoadScene, to load a scene on the main thread.
    /// </summary>
    /// <param name="sceneName">Scene name</param>
    private void LoadScene(string sceneName)
    {
        MainThreadDispatcher.RunOnMainThread(() => { SceneManager.LoadScene(sceneName); });
    }
    #endregion
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the ConnectedUsers Dictionary.
    /// </summary>
    /// <returns>ConnectedUsers Dictionary</returns>
    public Dictionary<Socket, UserData> GetConnectedUsers() { return connectedUsers; }
    #endregion
}