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
    private EGS_CL_Sockets socketsController; // TODO: Valorate if needed.


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
            EGS_Client.client_instance.connectedToServer = true;
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
        bool connectedToServer = EGS_Client.client_instance.connectedToServer;
        base.ReceiveCallback(ar, connectedToServer);
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

        // TODO: Maybe messageType as an enum?
        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            /*case "TEST_MESSAGE":
                Debug.Log("Received test message from server: " + receivedMessage.messageContent);
                break;*/
            case "CONNECT_TO_MASTER_SERVER":
                // Save as connected to the master server.
                EGS_Client.client_instance.connectedToMasterServer = true;

                // Create the user instance. // TODO: Assign it to the client data object.
                EGS_User thisUser = new EGS_User();
                thisUser.SetUsername(EGS_Client.client_instance.username);

                // Convert user to JSON.
                userJson = JsonUtility.ToJson(thisUser);

                messageToSend.messageType = "USER_JOIN_SERVER";
                messageToSend.messageContent = userJson;

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "RTT":
                // TODO: Save the time elapsed between RTTs.
                messageToSend.messageType = "RTT_RESPONSE_CLIENT";

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "DISCONNECT":
                // Close the socket to disconnect from the server.
                socketsController.CloseSocket();

                // Change scene to the MainMenu.
                LoadScene("MainMenu");

                // TODO: This should be done in a delegate, so programmer decides.
                break;
            case "JOIN_SERVER":
                // Get User Data.
                socketsController.thisUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);

                // Load new scene on main thread.
                LoadScene("MainMenu");

                // TODO: This should be done in a delegate, so programmer decides.
                break;
            case "GAME_FOUND":
                // Change scene to the GameLobby.
                LoadScene("GameLobby");
                // TODO: This should be done in a delegate, so programmer decides.

                EGS_UpdateData gameData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.messageContent);

                // Clear the dictionaries and add the new players.
                EGS_CL_Sockets.playerPositions.Clear();
                EGS_CL_Sockets.playerUsernames.Clear();

                foreach (EGS_PlayerData playerData in gameData.GetPlayersAtGame())
                {
                    EGS_CL_Sockets.playerPositions.Add(playerData.GetIngameID(), playerData.GetPosition());
                    EGS_CL_Sockets.playerUsernames.Add(playerData.GetIngameID(), playerData.GetUsername());
                }

                // TODO: Save Client Room.
                // room = gameData.GetRoom();
                break;
            case "CHANGE_TO_GAME_SERVER":
                // Construct the EndPoint to Game Server.
                string[] ep = receivedMessage.messageContent.Split(':');
                gameServerIP = ep[0];
                gameServerPort = int.Parse(ep[1]);

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
                EGS_Client.client_instance.connectedToMasterServer = false; // TODO: Check if this should be here.

                // Try to connect to Game Server.
                socketsController.ConnectToGameServer(gameServerIP, gameServerPort);
                break;
            case "CONNECT_GAME_SERVER":
                // Save as connected to the game server.
                EGS_Client.client_instance.connectedToGameServer = true;

                // Save the room.
                socketsController.thisUser.SetRoom(int.Parse(receivedMessage.messageContent));

                // Convert user to JSON.
                userJson = JsonUtility.ToJson(socketsController.thisUser);

                messageToSend.messageType = "JOIN_GAME_SERVER";
                messageToSend.messageContent = userJson;

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "JOIN_GAME_SERVER":
                // TODO: LoadGameScene, don't start game.
                //LoadScene("TestGame");
                break;
            case "GAME_START":
                // Load new scene on main thread.
                LoadScene("TestGame");
                // TODO: This should be done in a delegate, so programmer decides.
                break;
            case "UPDATE":
                //Debug.Log("Update MSG: " + receivedMessage.messageContent);
                EGS_UpdateData updateData = JsonUtility.FromJson<EGS_UpdateData>(receivedMessage.messageContent);

                foreach (EGS_PlayerData playerData in updateData.GetPlayersAtGame())
                {
                    EGS_CL_Sockets.playerPositions[playerData.GetIngameID()] = playerData.GetPosition();
                }

                // TODO: Delegate to to things on server update message.
                break;
            default:
                Debug.Log("<color=yellow>Undefined message type: </color>" + receivedMessage.messageType);
                EGS_ClientDelegates.onMessageReceive(receivedMessage);
                break;
        }
    }
    #endregion
}