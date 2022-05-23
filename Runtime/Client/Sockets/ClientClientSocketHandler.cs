using System;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class ClientClientSocketHandler, that controls the client sender socket.
/// </summary>
public class ClientClientSocketHandler : ClientSocketHandler
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Socket Manager")]
    private ClientSocketManager socketManager;


    [Header("Game Server Data")]
    [Tooltip("IP where the Game Server is")]
    private string gameServerIP;

    [Tooltip("Port where the Game Server is")]
    private int gameServerPort;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public ClientClientSocketHandler(ClientSocketManager socketManager) : base()
    {
        this.socketManager = socketManager;
    }
    #endregion

    #region Class Methods
    #region Networking
    /// <summary>
    /// Method ConnectCallback, called when connected to server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public override void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            base.ConnectCallback(ar);
        }
        catch (SocketException se)
        {
            // If SocketException error code is SocketError.ConnectionRefused.
            if (se.ErrorCode == 10061) // 10061 = SocketError.ConnectionRefused.
            {
                if (EasyGameServerConfig.DEBUG_MODE_CONSOLE >= EasyGameServerControl.EnumLogDebugLevel.Extended)
                    Debug.LogWarning("[CLIENT] Server refused the connection.");

                // Interrupt the connectionsThread.
                socketManager.GetConnectionsThread().Interrupt();

                // Try to connect to the server again.
                Client.instance.TryConnectToServerAgain();

                // Call the onServerRefusesConnection delegate.
                ClientDelegates.onServerRefusesConnection?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method ReceiveCallback, called when received data from server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    protected override void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            base.ReceiveCallback(ar);
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }
    #endregion

    /// <summary>
    /// Method HandleMessage, that receives a message from the server or game server and do things based on it.
    /// </summary>
    /// <param name="content">Message content</param>
    /// <param name="handler">Socket that handles that connection</param>
    protected override void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        NetworkMessage receivedMessage;

        try
        {
            receivedMessage = JsonUtility.FromJson<NetworkMessage>(content);
        }
        catch (Exception e)
        {
            Debug.LogWarning("ERORR, CONTENT: " + content);
            throw e;
        }

        if (EasyGameServerConfig.DEBUG_MODE_CONSOLE >= EasyGameServerControl.EnumLogDebugLevel.Complete)
            Debug.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.GetMessageType());

        // Message to send back.
        NetworkMessage messageToSend = new NetworkMessage();

        // Local variables that are used in the cases below.
        string jsonMSG;
        string userJson;
        UpdateData updateData;
        UserData thisUser;

        // Depending on the messageType, do different things.
        switch (receivedMessage.GetMessageType())
        {
            case ClientMessageTypes.RTT:
                // Save the client ping.
                long lastRTTMilliseconds = long.Parse(receivedMessage.GetMessageContent());
                Client.instance.SetClientPing(lastRTTMilliseconds);

                // Call the onRTT delegate.
                ClientDelegates.onRTT?.Invoke(lastRTTMilliseconds);

                // Prepare the message to send.
                messageToSend.SetMessageType(MasterServerMessageTypes.RTT_RESPONSE_CLIENT);
                messageToSend.SetMessageContent(Client.instance.GetUser().GetUserID().ToString());

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;

            case ClientMessageTypes.CONNECT_TO_MASTER_SERVER:
                // Save as connected to the master server.
                Client.instance.SetConnectedToMasterServer(true);
                Client.instance.SetConnectingToServer(false);

                // Call the onConnect delegate with type MasterServer.
                ClientDelegates.onConnect?.Invoke(EasyGameServerControl.EnumInstanceType.MasterServer);

                // Get the user instance.
                thisUser = Client.instance.GetUser();

                // Convert user to JSON.
                userJson = JsonUtility.ToJson(thisUser);

                // Construct the message.
                messageToSend.SetMessageType(MasterServerMessageTypes.USER_JOIN_SERVER);
                messageToSend.SetMessageContent(userJson);

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;

            case ClientMessageTypes.JOIN_MASTER_SERVER:
                // Get and update User Data.
                UserData updatedUser = JsonUtility.FromJson<UserData>(receivedMessage.GetMessageContent());
                Client.instance.SetUser(updatedUser);

                // Call the onJoinMasterServer delegate.
                ClientDelegates.onJoinMasterServer?.Invoke(updatedUser);
                break;
            case ClientMessageTypes.DISCONNECT:
                // Close the socket to disconnect from the server.
                socketManager.CloseSocket();

                // Change the connected to Master Server value.
                Client.instance.SetConnectedToMasterServer(false);

                // Call the onDisconnect delegate.
                ClientDelegates.onDisconnect?.Invoke();
                break;

            case ClientMessageTypes.GAME_FOUND:
                // Obtain GameFoundData.
                GameFoundData gameFoundData = JsonUtility.FromJson<GameFoundData>(receivedMessage.GetMessageContent());
                Client.instance.SetGameFoundData(gameFoundData);

                // Assign the ingame user ID.
                thisUser = gameFoundData.GetUsersToGame().Find(x => x.GetUserID() == Client.instance.GetUser().GetUserID());
                Client.instance.GetUser().SetIngameID(thisUser.GetIngameID());

                // Assign the room number.
                Client.instance.GetUser().SetRoom(gameFoundData.GetRoom());

                // Create and assign the new game data.
                updateData = new UpdateData(gameFoundData.GetRoom());
                foreach (UserData user in gameFoundData.GetUsersToGame())
                {
                    PlayerData playerData = new PlayerData(user.GetIngameID());
                    updateData.GetPlayersAtGame().Add(playerData);
                }
                
                Client.instance.SetGameData(updateData);

                // Execute code on game found.
                ClientDelegates.onGameFound?.Invoke(gameFoundData);
                break;

            case ClientMessageTypes.CHANGE_TO_GAME_SERVER:
                // Save the Game Server connection data (IP and port).
                string[] ep = receivedMessage.GetMessageContent().Split(':');
                gameServerIP = ep[0];
                gameServerPort = int.Parse(ep[1]);

                // Call the onPrepareToChangeFromMasterToGameServer delegate.
                ClientDelegates.onPrepareToChangeFromMasterToGameServer?.Invoke(gameServerIP, gameServerPort);

                // Tell the server that the client received the information so can connect to the game server.
                messageToSend.SetMessageType(MasterServerMessageTypes.DISCONNECT_TO_GAME);

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;

            case ClientMessageTypes.DISCONNECT_TO_GAME:
                // Close the socket to disconnect from the server.
                socketManager.CloseSocket();

                // Save as disconnected from the master server.
                Client.instance.SetConnectedToMasterServer(false);

                // Call the onChangeFromMasterToGameServer delegate.
                ClientDelegates.onChangeFromMasterToGameServer?.Invoke(gameServerIP, gameServerPort);

                // Try to connect to Game Server.
                MainThreadDispatcher.RunOnMainThread(() => socketManager.ConnectToGameServer(gameServerIP, gameServerPort));
                break;
            case ClientMessageTypes.CONNECT_TO_GAME_SERVER:
                // Save as connected to the game server.
                Client.instance.SetConnectedToGameServer(true);

                // Call the onConnect delegate with type GameServer.
                ClientDelegates.onConnect?.Invoke(EasyGameServerControl.EnumInstanceType.GameServer);

                // Convert user to JSON.
                userJson = JsonUtility.ToJson(Client.instance.GetUser());

                // Construct the message.
                messageToSend.SetMessageType(GameServerMessageTypes.JOIN_GAME_SERVER);
                messageToSend.SetMessageContent(userJson);

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;

            case ClientMessageTypes.JOIN_GAME_SERVER:
                // Call the onJoinMasterServer delegate.
                ClientDelegates.onJoinGameServer?.Invoke();
                break;

            case ClientMessageTypes.GAME_START:
                // Save the Update Data.
                updateData = JsonUtility.FromJson<UpdateData>(receivedMessage.GetMessageContent());
                Client.instance.SetGameData(updateData);

                // Create and assign the In Game Sender.
                ClientInGameSender thisInGameSender = new ClientInGameSender();
                Client.instance.SetInGameSender(thisInGameSender);

                // Start the In Game Sender.
                Client.instance.GetInGameSender().StartGameLoop();

                // Call the onGameStart delegate.
                ClientDelegates.onGameStart?.Invoke();
                break;

            case ClientMessageTypes.UPDATE:
                // Save the Update Data.
                updateData = JsonUtility.FromJson<UpdateData>(receivedMessage.GetMessageContent());
                Client.instance.GetGameData().SetPlayersAtGame(updateData.GetPlayersAtGame());

                // Call the onGameReceiveUpdate delegate.
                ClientDelegates.onGameReceiveUpdate?.Invoke(updateData);
                break;

            case ClientMessageTypes.PLAYER_LEAVE_GAME:
                // Get the Player ID from the message.
                int playerID = int.Parse(receivedMessage.GetMessageContent());

                // Get the Player Data and remove from GameData.
                PlayerData thisPlayerData = Client.instance.GetGameData().GetPlayersAtGame()[playerID];
                Client.instance.GetGameData().GetPlayersAtGame().Remove(thisPlayerData);

                // Call the onAnotherPlayerLeaveGame delegate.
                ClientDelegates.onAnotherPlayerLeaveGame?.Invoke(thisPlayerData);

                MainThreadDispatcher.RunOnMainThread(() => { Debug.Log("Player left Game: " + playerID); });
                break;

            case ClientMessageTypes.GAME_END:
                // Get the Game End Data and save it.
                GameEndData gameEndData = JsonUtility.FromJson<GameEndData>(receivedMessage.GetMessageContent());
                Client.instance.SetGameEndData(gameEndData);

                MainThreadDispatcher.RunOnMainThread(() => { Debug.Log("gameEndData.GetEndedAsDisconnection(): " + gameEndData.GetEndedAsDisconnection()); });

                // Stop the In Game Sender.
                Client.instance.GetInGameSender().StopGameLoop();

                // Call the onGameEnd delegate.
                ClientDelegates.onGameEnd?.Invoke(gameEndData);

                // Call the onPrepareToChangeFromGameToMasterServer delegate.
                ClientDelegates.onPrepareToChangeFromGameToMasterServer?.Invoke(EasyGameServerConfig.SERVER_IP, EasyGameServerConfig.SERVER_PORT);

                // DisconnectFromMasterServer from the Game Server and return to the MasterServer.
                messageToSend.SetMessageType(GameServerMessageTypes.RETURN_TO_MASTER_SERVER);

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;

            case ClientMessageTypes.DISCONNECT_TO_MASTER_SERVER:
                // Close the socket to disconnect from the server.
                socketManager.CloseSocket();

                // Save as disconnected from the master server.
                Client.instance.SetConnectedToGameServer(false);

                // Call the onChangeFromMasterToGameServer delegate.
                ClientDelegates.onChangeFromGameToMasterServer?.Invoke(EasyGameServerConfig.SERVER_IP, EasyGameServerConfig.SERVER_PORT);

                // Check if user left the game.
                bool userLeftGame = bool.Parse(receivedMessage.GetMessageContent());
                Client.instance.GetUser().SetLeftGame(userLeftGame);

                if (userLeftGame)
                {
                    // Call the onLeaveGame delegate.
                    ClientDelegates.onLeaveGame?.Invoke();
                }

                // Try to connect to Game Server.
                socketManager.ConnectToServer();
                break;

            case ClientMessageTypes.RETURN_TO_MASTER_SERVER:
                // Get and update User Data.
                thisUser = JsonUtility.FromJson<UserData>(receivedMessage.GetMessageContent());
                Client.instance.SetUser(thisUser);

                // Call the onReturnToMasterServer delegate.
                ClientDelegates.onReturnToMasterServer?.Invoke(thisUser);
                break;

            case ClientMessageTypes.CLOSE_SERVER:
                // Call the onServerClosed delegate.
                ClientDelegates.onServerClosed?.Invoke();
                break;

            case ClientMessageTypes.USER_DELETE:
                // Call the onUserDelete delegate.
                ClientDelegates.onUserDelete?.Invoke(Client.instance.GetUser());

                // Close the socket to disconnect from the server.
                socketManager.CloseSocket();

                // Save as disconnected from the master server.
                Client.instance.SetConnectedToMasterServer(false);
                break;

            default:
                // Call the onMessageReceive delegate.
                ClientDelegates.onMessageReceive?.Invoke(receivedMessage);
                break;
        }
    }
    #endregion
}