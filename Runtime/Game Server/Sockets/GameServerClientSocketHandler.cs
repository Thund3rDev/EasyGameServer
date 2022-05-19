using System;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class GameServerClientSocketHandler, that controls the client socket for the game server.
/// </summary>
public class GameServerClientSocketHandler : ClientSocketHandler
{
    #region Variables
    [Header("Networking")]
    [Tooltip("Socket Manager")]
    private GameServerSocketManager socketManager;
    #endregion

    #region Constructors
    /// <summary>
    /// Base constructor.
    /// </summary>
    public GameServerClientSocketHandler(GameServerSocketManager socketManager)
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
                    Debug.LogWarning("[GAME SERVER] Server refused the connection."); // LOG.

                // Interrupt the connectionsThread.
                socketManager.GetClientConnectionsThread().Interrupt();

                // Try to connect to the server again.
                GameServer.instance.TryConnectToServerAgain();

                // Call the onServerRefusesConnection delegate.
                GameServerDelegates.onServerRefusesConnection?.Invoke();
            }
        }
        catch (Exception e)
        {
            // LOG.
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
            // LOG.
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
        NetworkMessage receivedMessage;

        try
        {
            receivedMessage = JsonUtility.FromJson<NetworkMessage>(content);
        }
        catch (Exception e)
        {
            // LOG.
            Debug.LogWarning("ERORR, CONTENT: " + content);
            throw e;
        }

        // LOG.
        if (EasyGameServerConfig.DEBUG_MODE_CONSOLE >= EasyGameServerControl.EnumLogDebugLevel.Complete)
            Debug.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.GetMessageType());

        // Message to send back.
        NetworkMessage messageToSend = new NetworkMessage();

        // Local variables that are used in the cases below.
        string jsonMSG;
        string gameServerIP;

        // Depending on the messageType, do different things.
        switch (receivedMessage.GetMessageType())
        {
            case "RTT":
                // Save the client ping.
                long lastRTTMilliseconds = long.Parse(receivedMessage.GetMessageContent());
                GameServer.instance.SetClientPing(lastRTTMilliseconds);

                // Call the onRTT delegate.
                GameServerDelegates.onRTT?.Invoke(lastRTTMilliseconds);

                // Prepare the message to send.
                messageToSend.SetMessageType("RTT_RESPONSE_GAME_SERVER");
                messageToSend.SetMessageContent(GameServer.instance.GetGameServerID().ToString());

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "CONNECT_TO_MASTER_SERVER":
                // Save as connected to the master server.
                GameServer.instance.SetConnectedToMasterServer(true);

                // Call the onConnectToMasterServer delegate.
                GameServerDelegates.onConnectToMasterServer?.Invoke();

                // Send a message to the master server.
                messageToSend.SetMessageType("CREATED_GAME_SERVER");

                // Construct the gameServerIP to be sent:
                // TODO: Send as an object.
                gameServerIP = EasyGameServerConfig.SERVER_IP + ":" + GameServer.instance.GetGameServerPort();
                messageToSend.SetMessageContent(GameServer.instance.GetGameServerID() + "#" + gameServerIP);
                MainThreadDispatcher.RunOnMainThread(() => { GameServer.instance.console_text.text += "\nIPADRESS " + EasyGameServerConfig.SERVER_IP; });

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "RECEIVE_GAME_DATA":
                // Receive the GameFoundData.
                GameServer.instance.SetGameFoundData(JsonUtility.FromJson<GameFoundData>(receivedMessage.GetMessageContent()));

                // Change the server state.
                GameServer.instance.SetGameServerState(GameServerData.EnumGameServerState.WAITING_PLAYERS);
                MainThreadDispatcher.RunOnMainThread(() => { GameServer.instance.console_text.text += "\nStatus: " + Enum.GetName(typeof(GameServerData.EnumGameServerState), GameServer.instance.GetGameServerState()); });

                // Start listening for player connections and wait until it is started.
                socketManager.StartListening();
                socketManager.GetStartDoneMRE().WaitOne();

                // Call the onReadyToConnectPlayers delegate.
                GameServerDelegates.onReadyToConnectPlayers?.Invoke();

                // Construct the gameServerIP to be sent.
                gameServerIP = EasyGameServerConfig.SERVER_IP + ":" + GameServer.instance.GetGameServerPort();

                // Send a message to the master server.
                // TODO: Send as an object.
                messageToSend.SetMessageType("READY_GAME_SERVER");
                messageToSend.SetMessageContent(GameServer.instance.GetGameServerID() + "#" + gameServerIP);

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "DISCONNECT_AND_CLOSE_GAMESERVER":
                // Close the socket to disconnect from the server.
                socketManager.CloseClientSocket();

                // Save as disconnected from the master server.
                GameServer.instance.SetConnectedToMasterServer(false);

                // Trigger the UpdateDisconnected on the Game Server End Controller.
                MainThreadDispatcher.RunOnMainThread(() => { GameServerEndController.instance.UpdateDisconnectedFromMasterServer(); });
                break;
            case "MASTER_SERVER_CLOSE_GAME_SERVER":
                // Save as disconnected from the master server.
                GameServer.instance.SetConnectedToMasterServer(false);

                // Call the onMasterServerCloseGameServer delegate.
                GameServerDelegates.onMasterServerCloseGameServer?.Invoke();
                break;
            default:
                // Call the onServerMessageReceive delegate.
                GameServerDelegates.onServerMessageReceive?.Invoke(receivedMessage);
                break;
        }
    }
    #endregion
}