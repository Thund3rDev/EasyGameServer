using System;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class EGS_GS_ClientSocket, that controls the client socket for the game server.
/// </summary>
public class EGS_GS_ClientSocket : EGS_ClientSocket
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Sockets Controller")]
    private EGS_GS_Sockets socketsController;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public EGS_GS_ClientSocket(EGS_GS_Sockets socketsController_)
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
                if (EGS_Config.DEBUG_MODE > 1)
                    Debug.LogWarning("[GAME SERVER] Server refused the connection."); // LOG.

                // Interrupt the connectionsThread.
                socketsController.clientConnectionsThread.Interrupt();

                // Try to connect to the server again.
                EGS_GameServer.instance.TryConnectToServerAgain();

                // Call the onServerRefusesConnection delegate.
                EGS_GameServerDelegates.onServerRefusesConnection?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Game Server] " + e.ToString());
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
            Debug.LogError("[Game Server] " + e.ToString());
        }
    }
    #endregion

    /// <summary>
    /// Method HandleMessage, that receives a message from the server and do things based on it.
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
        string gameServerIP;

        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "RTT":
                // Save the client ping.
                long lastRTTMilliseconds = long.Parse(receivedMessage.messageContent);
                EGS_GameServer.instance.SetClientPing(lastRTTMilliseconds);

                // Call the onRTT delegate.
                EGS_GameServerDelegates.onRTT?.Invoke(lastRTTMilliseconds);

                // Prepare the message to send.
                messageToSend.messageType = "RTT_RESPONSE_GAME_SERVER";
                messageToSend.messageContent = EGS_GameServer.instance.gameServerID.ToString();

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "CONNECT_TO_MASTER_SERVER":
                // Save as connected to the master server.
                EGS_GameServer.instance.connectedToMasterServer = true;

                // Call the onConnectToMasterServer delegate.
                EGS_GameServerDelegates.onConnectToMasterServer?.Invoke();

                // Send a message to the master server.
                messageToSend.messageType = "CREATED_GAME_SERVER";

                // Construct the gameServerIP to be sent:
                // TODO: Send as an object.
                gameServerIP = EGS_Config.serverIP + ":" + EGS_GameServer.instance.gameServerPort;
                messageToSend.messageContent = EGS_GameServer.instance.gameServerID + "#" + gameServerIP;
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nIPADRESS " + EGS_Config.serverIP; });

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "RECEIVE_GAME_DATA":
                // Receive the GameFoundData.
                EGS_GameServer.instance.gameFoundData = JsonUtility.FromJson<EGS_GameFoundData>(receivedMessage.messageContent);

                // Change the server state.
                EGS_GameServer.instance.gameServerState = EGS_GameServerData.EGS_GameServerState.WAITING_PLAYERS;
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nStatus: " + Enum.GetName(typeof(EGS_GameServerData.EGS_GameServerState), EGS_GameServer.instance.gameServerState); });

                // Start listening for player connections and wait until it is started.
                socketsController.StartListening();
                socketsController.startDone.WaitOne();

                // Call the onReadyToConnectPlayers delegate.
                EGS_GameServerDelegates.onReadyToConnectPlayers?.Invoke();

                // Send a message to the master server.
                messageToSend.messageType = "READY_GAME_SERVER";

                // Construct the gameServerIP to be sent:
                // TODO: Send as an object.
                gameServerIP = EGS_Config.serverIP + ":" + EGS_GameServer.instance.gameServerPort;
                messageToSend.messageContent = EGS_GameServer.instance.gameServerID + "#" + gameServerIP;

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "DISCONNECT_AND_CLOSE_GAMESERVER":
                // Close the socket to disconnect from the server.
                socketsController.CloseClientSocket();

                // Save as disconnected from the master server.
                EGS_GameServer.instance.connectedToMasterServer = false;

                // Trigger the UpdateDisconnected on the Game Server End Controller.
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServerEndController.instance.UpdateDisconnectedFromMasterServer(); });
                break;
            case "MASTER_SERVER_CLOSE_GAME_SERVER":
                // Save as disconnected from the master server.
                EGS_GameServer.instance.connectedToMasterServer = false;

                // Call the onMasterServerCloseGameServer delegate.
                EGS_GameServerDelegates.onMasterServerCloseGameServer?.Invoke();
                break;
            default:
                // Call the onServerMessageReceive delegate.
                EGS_GameServerDelegates.onServerMessageReceive?.Invoke(receivedMessage);
                break;
        }
    }
    #endregion
}