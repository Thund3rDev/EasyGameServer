using System;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class EGS_CL_ClientSocket, that controls the client sender socket.
/// </summary>
public class EGS_CL_ClientSocket : EGS_ClientSocket
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Sockets Controller")]
    private EGS_CL_Sockets socketsController;


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
    public EGS_CL_ClientSocket(EGS_CL_Sockets socketsController_)
    {
        socketsController = socketsController_;
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
                if (EGS_Config.DEBUG_MODE_CONSOLE >= EGS_Control.EGS_DebugLevel.Extended)
                    Debug.LogWarning("[CLIENT] Server refused the connection.");

                // Interrupt the connectionsThread.
                socketsController.connectionsThread.Interrupt();

                // Try to connect to the server again.
                EGS_Client.instance.TryConnectToServerAgain();

                // Call the onServerRefusesConnection delegate.
                EGS_ClientDelegates.onServerRefusesConnection?.Invoke();
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
        EGS_Message receivedMessage;

        try
        {
            receivedMessage = JsonUtility.FromJson<EGS_Message>(content);
        }
        catch (Exception e)
        {
            Debug.LogWarning("ERORR, CONTENT: " + content);
            throw e;
        }

        if (EGS_Config.DEBUG_MODE_CONSOLE >= EGS_Control.EGS_DebugLevel.Complete)
            Debug.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.GetMessageType());

        // Message to send back.
        EGS_Message messageToSend = new EGS_Message();

        // Local variables that are used in the cases below.
        string jsonMSG;
        string userJson;
        EGS_UpdateData updateData;
        EGS_User thisUser;

        // TODO: Maybe messageType (EGS only) as an enum?
        // Depending on the messageType, do different things.
        switch (receivedMessage.GetMessageType())
        {
            case "RTT":
                // Save the client ping.
                long lastRTTMilliseconds = long.Parse(receivedMessage.GetMessageContent());
                EGS_Client.instance.SetClientPing(lastRTTMilliseconds);

                // Call the onRTT delegate.
                EGS_ClientDelegates.onRTT?.Invoke(lastRTTMilliseconds);

                // Prepare the message to send.
                messageToSend.SetMessageType("RTT_RESPONSE_CLIENT");
                messageToSend.SetMessageContent(EGS_Client.instance.GetUser().GetUserID().ToString());

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "CONNECT_TO_MASTER_SERVER":
                // Save as connected to the master server.
                EGS_Client.instance.connectedToMasterServer = true;
                EGS_Client.instance.connectingToServer = false;

                // Call the onConnect delegate with type MasterServer.
                EGS_ClientDelegates.onConnect?.Invoke(EGS_Control.EGS_Type.MasterServer);

                // Get the user instance.
                thisUser = EGS_Client.instance.GetUser();

                // Convert user to JSON.
                userJson = JsonUtility.ToJson(thisUser);

                // Construct the message.
                messageToSend.SetMessageType("USER_JOIN_SERVER");
                messageToSend.SetMessageContent(userJson);

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "JOIN_MASTER_SERVER":
                // Get and update User Data.
                EGS_User updatedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.GetMessageContent());
                EGS_Client.instance.SetUser(updatedUser);

                // Call the onJoinMasterServer delegate.
                EGS_ClientDelegates.onJoinMasterServer?.Invoke(updatedUser);
                break;
            case "DISCONNECT":
                // Close the socket to disconnect from the server.
                socketsController.CloseSocket();

                // Change the connected to Master Server value.
                EGS_Client.instance.connectedToMasterServer = false;

                // Call the onDisconnect delegate.
                EGS_ClientDelegates.onDisconnect?.Invoke();
                break;
            case "GAME_FOUND":
                // Obtain GameFoundData.
                EGS_GameFoundData gameFoundData = JsonUtility.FromJson<EGS_GameFoundData>(receivedMessage.GetMessageContent());
                EGS_Client.instance.SetGameFoundData(gameFoundData);

                // Assign the ingame user ID.
                thisUser = gameFoundData.GetUsersToGame().Find(x => x.GetUserID() == EGS_Client.instance.GetUser().GetUserID());
                EGS_Client.instance.GetUser().SetIngameID(thisUser.GetIngameID());

                // Assign the room number.
                EGS_Client.instance.GetUser().SetRoom(gameFoundData.GetRoom());

                // Create and assign the new game data.
                updateData = new EGS_UpdateData(gameFoundData.GetRoom());
                foreach (EGS_User user in gameFoundData.GetUsersToGame())
                {
                    EGS_PlayerData playerData = new EGS_PlayerData(user.GetIngameID());
                    updateData.GetPlayersAtGame().Add(playerData);
                }
                
                EGS_Client.instance.SetGameData(updateData);

                // Execute code on game found.
                EGS_ClientDelegates.onGameFound?.Invoke(gameFoundData);
                break;
            case "CHANGE_TO_GAME_SERVER":
                // Save the Game Server connection data (IP and port).
                string[] ep = receivedMessage.GetMessageContent().Split(':');
                gameServerIP = ep[0];
                gameServerPort = int.Parse(ep[1]);

                // Call the onPrepareToChangeFromMasterToGameServer delegate.
                EGS_ClientDelegates.onPrepareToChangeFromMasterToGameServer?.Invoke(gameServerIP, gameServerPort);

                // Tell the server that the client received the information so can connect to the game server.
                messageToSend.SetMessageType("DISCONNECT_TO_GAME");

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "DISCONNECT_TO_GAME":
                // Close the socket to disconnect from the server.
                socketsController.CloseSocket();

                // Save as disconnected from the master server.
                EGS_Client.instance.connectedToMasterServer = false;

                // Call the onChangeFromMasterToGameServer delegate.
                EGS_ClientDelegates.onChangeFromMasterToGameServer?.Invoke(gameServerIP, gameServerPort);

                // Try to connect to Game Server.
                socketsController.ConnectToGameServer(gameServerIP, gameServerPort);
                break;
            case "CONNECT_TO_GAME_SERVER":
                // Save as connected to the game server.
                EGS_Client.instance.connectedToGameServer = true;

                // Call the onConnect delegate with type GameServer.
                EGS_ClientDelegates.onConnect?.Invoke(EGS_Control.EGS_Type.GameServer);

                // Convert user to JSON.
                userJson = JsonUtility.ToJson(EGS_Client.instance.GetUser());

                // Construct the message.
                messageToSend.SetMessageType("JOIN_GAME_SERVER");
                messageToSend.SetMessageContent(userJson);

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "JOIN_GAME_SERVER":
                // Call the onJoinMasterServer delegate.
                EGS_ClientDelegates.onJoinGameServer?.Invoke();
                break;
            case "GAME_START":
                // Save the Update Data.
                updateData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.GetMessageContent());
                EGS_Client.instance.SetGameData(updateData);

                // Create and assign the In Game Sender.
                EGS_CL_InGameSender thisInGameSender = new EGS_CL_InGameSender();
                EGS_Client.instance.SetInGameSender(thisInGameSender);

                // Start the In Game Sender.
                EGS_Client.instance.GetInGameSender().StartGameLoop();

                // Call the onGameStart delegate.
                EGS_ClientDelegates.onGameStart?.Invoke();
                break;
            case "UPDATE":
                // Save the Update Data.
                updateData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.GetMessageContent());
                EGS_Client.instance.GetGameData().SetPlayersAtGame(updateData.GetPlayersAtGame());

                // Call the onGameReceiveUpdate delegate.
                EGS_ClientDelegates.onGameReceiveUpdate?.Invoke(updateData);
                break;
            case "PLAYER_LEAVE_GAME":
                // Get the Player ID from the message.
                int playerID = int.Parse(receivedMessage.GetMessageContent());

                // Get the Player Data and remove from GameData.
                EGS_PlayerData thisPlayerData = EGS_Client.instance.GetGameData().GetPlayersAtGame()[playerID];
                EGS_Client.instance.GetGameData().GetPlayersAtGame().Remove(thisPlayerData);

                // Call the onAnotherPlayerLeaveGame delegate.
                EGS_ClientDelegates.onAnotherPlayerLeaveGame?.Invoke(thisPlayerData);

                EGS_Dispatcher.RunOnMainThread(() => { Debug.Log("Player left Game: " + playerID); });
                break;
            case "GAME_END":
                // Get the Game End Data and save it.
                EGS_GameEndData gameEndData = JsonUtility.FromJson<EGS_GameEndData>(receivedMessage.GetMessageContent());
                EGS_Client.instance.SetGameEndData(gameEndData);

                EGS_Dispatcher.RunOnMainThread(() => { Debug.Log("gameEndData.GetEndedAsDisconnection(): " + gameEndData.GetEndedAsDisconnection()); });

                // Stop the In Game Sender.
                EGS_Client.instance.GetInGameSender().StopGameLoop();

                // Call the onGameEnd delegate.
                EGS_ClientDelegates.onGameEnd?.Invoke(gameEndData);

                // Call the onPrepareToChangeFromGameToMasterServer delegate.
                EGS_ClientDelegates.onPrepareToChangeFromGameToMasterServer?.Invoke(EGS_Config.serverIP, EGS_Config.serverPort);

                // Disconnect from the Game Server and return to the MasterServer.
                messageToSend.SetMessageType("RETURN_TO_MASTER_SERVER");

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "DISCONNECT_TO_MASTER_SERVER":
                // Close the socket to disconnect from the server.
                socketsController.CloseSocket();

                // Save as disconnected from the master server.
                EGS_Client.instance.connectedToGameServer = false;

                // Call the onChangeFromMasterToGameServer delegate.
                EGS_ClientDelegates.onChangeFromGameToMasterServer?.Invoke(EGS_Config.serverIP, EGS_Config.serverPort);

                // Check if user left the game.
                bool userLeftGame = bool.Parse(receivedMessage.GetMessageContent());
                EGS_Client.instance.GetUser().SetLeftGame(userLeftGame);

                if (userLeftGame)
                {
                    // Call the onLeaveGame delegate.
                    EGS_ClientDelegates.onLeaveGame?.Invoke();
                }

                // Try to connect to Game Server.
                socketsController.ConnectToServer();
                break;
            case "RETURN_TO_MASTER_SERVER":
                // Get and update User Data.
                thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.GetMessageContent());
                EGS_Client.instance.SetUser(thisUser);

                // Call the onReturnToMasterServer delegate.
                EGS_ClientDelegates.onReturnToMasterServer?.Invoke(thisUser);
                break;
            default:
                // Call the onMessageReceive delegate.
                EGS_ClientDelegates.onMessageReceive?.Invoke(receivedMessage);
                break;
        }
    }
    #endregion
}