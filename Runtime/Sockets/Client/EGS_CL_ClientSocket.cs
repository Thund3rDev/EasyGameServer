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

        if (EGS_Config.DEBUG_MODE > 2)
            Debug.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);

        // Message to send back.
        EGS_Message messageToSend = new EGS_Message();

        // Local variables that are used in the cases below.
        string jsonMSG;
        string userJson;
        EGS_UpdateData updateData;

        // TODO: Maybe messageType (EGS only) as an enum?
        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "RTT":
                // Save the client ping.
                long lastRTTMilliseconds = long.Parse(receivedMessage.messageContent);
                EGS_Client.instance.SetClientPing(lastRTTMilliseconds);

                // Call the onRTT delegate.
                EGS_ClientDelegates.onRTT?.Invoke(lastRTTMilliseconds);

                // Prepare the message to send.
                messageToSend.messageType = "RTT_RESPONSE_CLIENT";
                messageToSend.messageContent = EGS_Client.instance.GetUser().GetUserID().ToString();

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "CONNECT_TO_MASTER_SERVER":
                // Save as connected to the master server.
                EGS_Client.instance.connectedToMasterServer = true;

                // Call the onConnect delegate with type MasterServer.
                EGS_ClientDelegates.onConnect?.Invoke(EGS_Control.EGS_Type.MasterServer);

                // Get the user instance.
                EGS_User thisUser = EGS_Client.instance.GetUser();

                // Convert user to JSON.
                userJson = JsonUtility.ToJson(thisUser);

                messageToSend.messageType = "USER_JOIN_SERVER";
                messageToSend.messageContent = userJson;

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "JOIN_MASTER_SERVER":
                // Get and update User Data.
                EGS_User updatedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
                EGS_Client.instance.SetUser(updatedUser);

                // Call the onJoinMasterServer delegate.
                EGS_ClientDelegates.onJoinMasterServer?.Invoke(updatedUser);
                break;
            case "DISCONNECT":
                // Close the socket to disconnect from the server.
                socketsController.CloseSocket();

                // Call the onDisconnect delegate.
                EGS_ClientDelegates.onDisconnect?.Invoke();
                break;
            case "GAME_FOUND":
                // Obtain GameFoundData.
                EGS_GameFoundData gameFoundData = JsonUtility.FromJson<EGS_GameFoundData>(receivedMessage.messageContent);
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
                string[] ep = receivedMessage.messageContent.Split(':');
                gameServerIP = ep[0];
                gameServerPort = int.Parse(ep[1]);

                // Call the onPrepareToChangeFromMasterToGameServer delegate.
                EGS_ClientDelegates.onPrepareToChangeFromMasterToGameServer?.Invoke(gameServerIP, gameServerPort);

                // Tell the server that the client received the information so can connect to the game server.
                messageToSend.messageType = "DISCONNECT_TO_GAME";

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

                messageToSend.messageType = "JOIN_GAME_SERVER";
                messageToSend.messageContent = userJson;

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
                Debug.Log(receivedMessage.messageContent);
                // Save the Update Data.
                updateData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.messageContent);
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
                updateData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.messageContent);
                EGS_Client.instance.GetGameData().SetPlayersAtGame(updateData.GetPlayersAtGame());

                // Call the onGameReceiveUpdate delegate.
                EGS_ClientDelegates.onGameReceiveUpdate?.Invoke(updateData);
                break;
            case "GAME_END":
                // Stop the In Game Sender.
                EGS_Client.instance.GetInGameSender().StopGameLoop();

                // Call the onGameEnd delegate.
                EGS_ClientDelegates.onGameEnd?.Invoke();
                break;
            default:
                // Call the onMessageReceive delegate.
                EGS_ClientDelegates.onMessageReceive?.Invoke(receivedMessage);
                break;
        }
    }
    #endregion
}