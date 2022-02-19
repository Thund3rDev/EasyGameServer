using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
            EGS_GameServer.gameServer_instance.connectedToMasterServer = true;
        }
        catch(Exception e)
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
        bool connectedToServer = EGS_GameServer.gameServer_instance.connectedToMasterServer;
        base.ReceiveCallback(ar, connectedToServer);
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

        // Depending on the messageType, do different things.
        switch (receivedMessage.messageType)
        {
            case "TEST_MESSAGE":
                Debug.Log("Received test message from server: " + receivedMessage.messageContent);
                break;
            case "RTT":
                // TODO: Save the time elapsed between RTTs.
                messageToSend.messageType = "RTT_RESPONSE_GAME_SERVER";
                messageToSend.messageContent = EGS_GameServer.gameServer_instance.gameServerID.ToString();

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "CONNECT_TO_MASTER_SERVER":
                // Change the server state.
                EGS_GameServer.gameServer_instance.gameServerState = EGS_GameServerData.EGS_GameServerState.CREATED;
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text = "Status: " + Enum.GetName(typeof(EGS_GameServerData.EGS_GameServerState), EGS_GameServer.gameServer_instance.gameServerState); });

                // Start listening for player connections and wait until it is started.
                socketsController.StartListening();
                socketsController.startDone.WaitOne();

                // Send a message to the master server.
                messageToSend.messageType = "CREATED_GAME_SERVER";

                string gameServerIP = EGS_Config.serverIP + ":" + EGS_GameServer.gameServer_instance.gameServerPort;
                messageToSend.messageContent = EGS_GameServer.gameServer_instance.gameServerID + "#" + gameServerIP;
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.gameServer_instance.test_text.text += "\nIPADRESS " + EGS_Config.serverIP; });

                // Convert message to JSON.
                jsonMSG = messageToSend.ConvertMessage();

                // Send data to server.
                Send(handler, jsonMSG);
                break;
            case "CLOSE_GAME_SERVER":
                EGS_GameServer.gameServer_instance.connectedToMasterServer = false;
                break;
            default:
                Debug.Log("<color=yellow>Undefined message type: </color>" + receivedMessage.messageType);
                break;
        }
    }
    #endregion
}