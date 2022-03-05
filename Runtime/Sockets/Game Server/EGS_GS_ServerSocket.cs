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
        EGS_User receivedUser;
        EGS_Player thisPlayer;

        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "RTT_RESPONSE_CLIENT":
                long rttPing = roundTripTimes[handler].ReceiveRTT();
                // TODO: Update UI? I think it is better on UI Update method.
                break;
            case "JOIN_GAME_SERVER":
                try
                {
                    // Get the received user
                    receivedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
                    receivedUser.SetSocket(handler);

                    // If the user is on the list to play this game.
                    if (allUsers.ContainsKey(receivedUser.GetUserID()))
                    {
                        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nPLAYER JOINED: " + receivedUser.GetUsername(); });
                            
                        // Connect the user.
                        ConnectUser(receivedUser, handler);

                        // Put a heartbeat for the client socket.
                        CreateRTT(handler);

                        // Echo the data back to the client.
                        messageToSend.messageType = "JOIN_GAME_SERVER";
                        jsonMSG = messageToSend.ConvertMessage();
                        Send(handler, jsonMSG);

                        // Check if game started / are all players.
                        // TODO: Only prepare the game, not start it.
                        bool startedGame = EGS_GameServer.instance.thisGame.Ready();
                        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nStartedGame: " + startedGame; });

                        if (startedGame)
                        {
                            // TODO: Delegates.

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

                EGS_GameServer.instance.thisGame.QuitPlayerFromGame(leftPlayer);
                break;
            default:
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nUndefined message type: " + receivedMessage.messageType; });
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