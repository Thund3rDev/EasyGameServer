using System;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Class GameServerDelegates, that contains the delegates that can be used on the game server side on specific moments.
/// </summary>
public static class GameServerDelegates
{
    #region Delegates
    [Header("General Delegates")]
    [Tooltip("Delegate to the OnMasterServerMessageReceive function")]
    public static Action<NetworkMessage> onMasterServerMessageReceive;

    [Tooltip("Delegate to the OnClientMessageReceive function")]
    public static Action<NetworkMessage, GameServerServerSocketHandler, Socket> onClientMessageReceive;


    [Header("Game Server Control Delegates")]
    [Tooltip("Delegate to the OnGameServerCreated function")]
    public static Action onGameServerCreated;

    [Tooltip("Delegate to the OnGameServerShutdown function")]
    public static Action onGameServerShutdown;

    // FUTURE (actually not working).
    [Tooltip("Delegate to the OnMasterServerCloseGameServer function")]
    public static Action onMasterServerCloseGameServer;


    [Header("Client Socket Delegates")]
    [Tooltip("Delegate to the OnMasterServerRefusesConnection function")]
    public static Action onMasterServerRefusesConnection;

    [Tooltip("Delegate to the OnCantConnectToMasterServer function")]
    public static Action onCantConnectToMasterServer;

    [Tooltip("Delegate to the OnConnectToMasterServer function")]
    public static Action onConnectToMasterServer;

    [Tooltip("Delegate to the OnReadyToConnectPlayers function")]
    public static Action onReadyToConnectPlayers;


    [Header("User Control Delegates")]
    [Tooltip("Delegate to the OnUserJoinServer function")]
    public static Action<UserData> onUserJoinServer;

    [Tooltip("Delegate to the OnUserConnect function")]
    public static Action<UserData> onUserConnect;

    [Tooltip("Delegate to the OnUserDisconnect function")]
    public static Action<UserData> onUserDisconnect;

    [Tooltip("Delegate to the OnUserDisconnectToMasterServer function")]
    public static Action<UserData> onUserDisconnectToMasterServer;


    [Header("Moment Delegates")]
    [Tooltip("Delegate to the OnAllPlayersConnected function")]
    public static Action onAllPlayersConnected;

    [Tooltip("Delegate to the OnGameStart function")]
    public static Action<UpdateData> onGameStart;

    [Tooltip("Delegate to the OnGameEnd function")]
    public static Action<GameEndData> onGameEnd;


    [Header("Player Delegates")]
    [Tooltip("Delegate to the OnPlayerSendInput function")]
    public static Action<NetworkPlayer, PlayerInputs> onPlayerSendInput;

    [Tooltip("Delegate to the OnPlayerLeaveGame function")]
    public static Action<NetworkPlayer> onPlayerLeaveGame;


    [Header("Game Delegates")]
    [Tooltip("Delegate to the OnTick function")]
    public static Action<UpdateData> onTick;

    [Tooltip("Delegate to the OnProcessPlayer function")]
    public static Action<NetworkPlayer, UpdateData, long> onProcessPlayer; // long: TICK_RATE.


    [Header("Control Delegates")]
    [Tooltip("Delegate to the OnRTT function")]
    public static Action<long> onRTT; // long: milliseconds.

    [Tooltip("Delegate to the OnReceiveClientRTT function")]
    public static Action<int, long> onReceiveClientRTT; // int: Client ID, long: milliseconds.
    #endregion
}
