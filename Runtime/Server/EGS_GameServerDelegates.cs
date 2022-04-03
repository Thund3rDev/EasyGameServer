using System;
using System.Net.Sockets;
using UnityEngine;

public static class EGS_GameServerDelegates
{
    #region Variables
    [Header("General Delegates")]
    [Tooltip("Delegate to the OnServerMessageReceive function")]
    public static Action<EGS_Message> onServerMessageReceive;

    [Tooltip("Delegate to the OnClientMessageReceive function")]
    public static Action<EGS_Message, EGS_GS_ServerSocket, Socket> onClientMessageReceive;


    [Header("Game Server Control Delegates")]
    [Tooltip("Delegate to the OnGameServerCreated function")]
    public static Action onGameServerCreated;

    [Tooltip("Delegate to the OnGameServerShutdown function")]
    public static Action onGameServerShutdown;

    [Tooltip("Delegate to the OnMasterServerCloseGameServer function")]
    public static Action onMasterServerCloseGameServer;


    [Header("Client Socket Delegates")]
    [Tooltip("Delegate to the OnConnectToMasterServer function")]
    public static Action onConnectToMasterServer;

    [Tooltip("Delegate to the OnReadyToConnectPlayers function")]
    public static Action onReadyToConnectPlayers;


    [Header("User Control Delegates")]
    [Tooltip("Delegate to the OnUserJoinServer function")]
    public static Action<EGS_User> onUserJoinServer;

    [Tooltip("Delegate to the OnUserConnect function")]
    public static Action<EGS_User> onUserConnect;

    [Tooltip("Delegate to the OnUserDisconnect function")]
    public static Action<EGS_User> onUserDisconnect;


    [Header("Moment Delegates")]
    [Tooltip("Delegate to the OnAllPlayersConnected function")]
    public static Action onAllPlayersConnected;

    [Tooltip("Delegate to the OnGameStart function")]
    public static Action onGameStart;


    [Header("Player Delegates")]
    [Tooltip("Delegate to the OnPlayerSendInput function")]
    public static Action<EGS_Player, EGS_PlayerInputs> onPlayerSendInput;

    [Tooltip("Delegate to the OnPlayerLeaveGame function")]
    public static Action<EGS_Player> onPlayerLeaveGame;


    [Header("Game Delegates")]
    [Tooltip("Delegate to the OnTick function")]
    public static Action<EGS_UpdateData> onTick;

    [Tooltip("Delegate to the OnProcessPlayer function")]
    public static Action<EGS_Player, EGS_UpdateData, long> onProcessPlayer;


    [Header("Control Delegates")]
    [Tooltip("Delegate to the OnRTT function")]
    public static Action<long> onRTT;

    [Tooltip("Delegate to the OnReceiveClientRTT function")]
    public static Action<int, long> onReceiveClientRTT;
    #endregion
}
