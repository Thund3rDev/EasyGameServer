using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class EGS_GS_ServerSocket, that controls the server receiver socket.
/// </summary>
public class EGS_GS_ServerSocket : EGS_ServerSocket
{
    #region Variables
    [Header("References")]
    [Tooltip("Reference to the sockets controller")]
    private EGS_GS_Sockets socketsController;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public EGS_GS_ServerSocket(EGS_GS_Sockets socketsController_)
    {
        socketsController = socketsController_;

        // Get the info of users to this game.
        foreach (EGS_User userToGame in EGS_GameServer.instance.gameFoundData.GetUsersToGame())
        {
            allUsers.Add(userToGame.GetUserID(), userToGame);
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
    public override void StartListening(EndPoint localEP, Socket socket_listener, int connections)
    {
        base.StartListening(localEP, socket_listener, connections);
        socketsController.startDone.Set();
    }
    #endregion

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
            EGS_Dispatcher.RunOnMainThread(()=> { EGS_GameServer.instance.test_text.text += ("\nPError parsing receivedMessage from JSON: " + e.StackTrace); });
            throw e;
        }

        // Message to send back.
        EGS_Message messageToSend = new EGS_Message();

        // Local variables that are used in the cases below.
        string jsonMSG;
        EGS_User thisUser;
        EGS_Player thisPlayer;
        long rttPing;

        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "RTT_RESPONSE_CLIENT":
                // Get the needed data.
                rttPing = roundTripTimes[handler].ReceiveRTT();
                thisUser = connectedUsers[handler];

                // TODO: Log in the Game Server Console.
                /*if (EGS_Config.DEBUG_MODE > 2)
                    egs_Log.Log("<color=blue>Round Trip Time (Client):</color> " + thisUser.GetUsername() + " (" + rttPing + " ms).");*/

                // Call the onReceiveClientRTT delegate with UserID and the rtt ping in milliseconds.
                EGS_GameServerDelegates.onReceiveClientRTT?.Invoke(thisUser.GetUserID(), rttPing);

                // TODO: Update UI? I think it is better on UI Update method or in the delegate.
                break;
            case "JOIN_GAME_SERVER":
                try
                {
                    // Get the received user
                    thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
                    thisUser.SetSocket(handler);

                    // If the user is on the list to play this game.
                    if (allUsers.ContainsKey(thisUser.GetUserID()))
                    {
                        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nPLAYER JOINED: " + thisUser.GetUsername(); });
                            
                        // Connect the user.
                        ConnectUser(thisUser, handler);

                        // Put a heartbeat for the client socket.
                        CreateRTT(handler);

                        // Echo the data back to the client.
                        messageToSend.messageType = "JOIN_GAME_SERVER";
                        jsonMSG = messageToSend.ConvertMessage();
                        Send(handler, jsonMSG);

                        // Call the onUserJoinServer delegate.
                        EGS_GameServerDelegates.onUserJoinServer?.Invoke(thisUser);

                        // Check if game started / are all players.
                        // TODO: Only prepare the game, not start it.
                        bool allPlayersConnected = EGS_GameServer.instance.thisGame.Ready();
                        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nAllPlayersConnected: " + allPlayersConnected; });

                        if (allPlayersConnected)
                        {
                            // Call the onAllPlayersConnected delegate.
                            EGS_GameServerDelegates.onAllPlayersConnected?.Invoke();

                            // Load the Game Scene.
                            LoadScene(EGS_GameServer.instance.thisGame.GetGameSceneName());

                            // Put an event to execute on Game Scene Load.
                            SceneManager.sceneLoaded += OnGameSceneLoad;
                        }
                    }
                }
                catch (Exception e)
                {
                    EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nEXCEPTION: " + e.ToString(); });
                }
                break;
            case "DISCONNECT_USER":
                // Get the received user
                thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);

                DisconnectUser(thisUser);
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

                // Call the onPlayerSendInput delegate.
                EGS_GameServerDelegates.onPlayerSendInput?.Invoke(thisPlayer);
                break;
            case "LEAVE_GAME":
                // Get the player.
                int playerID = int.Parse(receivedMessage.messageContent);
                thisPlayer = EGS_GameManager.instance.GetPlayersByID()[playerID];

                // Remove the player from the game.
                EGS_GameManager.instance.GetPlayersByID().Remove(playerID);
                EGS_GameServer.instance.thisGame.QuitPlayerFromGame(thisPlayer);

                // Call the onPlayerLeaveGame delegate.
                EGS_GameServerDelegates.onPlayerLeaveGame?.Invoke(thisPlayer);
                break;
            default:
                // Call the onClientMessageReceive delegate.
                EGS_GameServerDelegates.onClientMessageReceive?.Invoke(receivedMessage);
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
        // Ask client for user data.
        EGS_Message msg = new EGS_Message();
        msg.messageType = "CONNECT_TO_GAME_SERVER";
        string jsonMSG = msg.ConvertMessage();

        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nPLAYER CONNECTED: " + client_socket.RemoteEndPoint; });
        Send(client_socket, jsonMSG);
    }

    /// <summary>
    /// Method OnClientDisconnected, that manages a disconnection.
    /// </summary>
    /// <param name="client_socket">Client socket disconnected from the server</param>
    public override void OnClientDisconnected(Socket client_socket)
    {
        // TODO: Make this work.
    }
    #endregion

    #region User Management Methods
    /// <summary>
    /// Method ConnectUser, that connects an user to the server.
    /// </summary>
    /// <param name="userToConnect">User to connect to the server</param>
    /// <param name="client_socket">Socket that handles the client connection</param>
    protected override void ConnectUser(EGS_User userToConnect, Socket client_socket)
    {
        base.ConnectUser(userToConnect, client_socket);

        // Display data on the console. // LOG.
        //if (EGS_Config.DEBUG_MODE > -1)
            //egs_Log.Log("<color=purple>Connected User</color>: UserID: " + userToConnect.GetUserID() + " - Username: " + userToConnect.GetUsername() + " - IP: " + client_socket.RemoteEndPoint + ".");

        // Call the onUserConnect delegate.
        EGS_GameServerDelegates.onUserConnect?.Invoke(userToConnect);
    }

    /// <summary>
    /// Method DisconnectUser, that disconnects an user from the server.
    /// </summary>
    /// <param name="userToDisconnect">User to disconnect from the server</param>
    protected override void DisconnectUser(EGS_User userToDisconnect)
    {
        base.DisconnectUser(userToDisconnect);

        // Display data on the console. // LOG.
        //if (EGS_Config.DEBUG_MODE > -1)
            //egs_Log.Log("<color=purple>Disconnected User</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetSocket().RemoteEndPoint + ".");

        // Call the onUserDisconnect delegate.
        EGS_GameServerDelegates.onUserDisconnect?.Invoke(userToDisconnect);
    }
    #endregion

    private void OnGameSceneLoad(Scene s, LoadSceneMode ls)
    {
        if (s.name.Equals(EGS_GameServer.instance.thisGame.GetGameSceneName()))
            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServerDelegates.onGameStart(); });

        // TODO: Send to the master server the info of the started game.

        EGS_Message messageToSend = new EGS_Message();
        messageToSend.messageType = "GAME_START";
        messageToSend.messageContent = "";

        string jsonMSG = messageToSend.ConvertMessage();

        string playersString = "";
        foreach (EGS_User user in EGS_GameServer.instance.gameFoundData.GetUsersToGame())
        {
            playersString += user.GetUsername() + ", ";
        }

        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\n" + EGS_GameServer.instance.thisGame.GetPlayers().Count + " | " + playersString; });
        foreach (EGS_User user in EGS_GameServer.instance.gameFoundData.GetUsersToGame())
        {
            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nSEND TO : " + user.GetUsername(); });
            Send(user.GetSocket(), jsonMSG);
        }

        // Call the onGameStart delegate.
        EGS_GameServerDelegates.onGameStart?.Invoke();
    }

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