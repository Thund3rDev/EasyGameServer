using System;
using UnityEngine;

/// <summary>
/// Class ClientDelegates, that contains the delegates that can be used on the client side on specific moments.
/// </summary>
public static class ClientDelegates
{
    #region Delegates
    [Header("General Delegates")]
    [Tooltip("Delegate to the OnMessageReceive function")]
    public static Action<NetworkMessage> onMessageReceive;

    [Tooltip("Delegate to the OnUserCreate function")]
    public static Action<UserData> onUserCreate;


    [Header("Connect Delegates")]
    [Tooltip("Delegate to the OnServerRefusesConnection function")]
    public static Action onServerRefusesConnection;

    [Tooltip("Delegate to the OnCantConnectToServer function")]
    public static Action onCantConnectToServer;

    [Tooltip("Delegate to the OnConnect function")]
    public static Action<EasyGameServerControl.EnumInstanceType> onConnect;

    [Tooltip("Delegate to the OnJoinMasterServer function")]
    public static Action<UserData> onJoinMasterServer;

    [Tooltip("Delegate to the OnJoinGameServer function")]
    public static Action onJoinGameServer;

    [Tooltip("Delegate to the OnDisconnect function")]
    public static Action onDisconnect;

    [Tooltip("Delegate to the OnUserDelete function")]
    public static Action<UserData> onUserDelete;

    [Tooltip("Delegate to the OnPrepareToChangeFromMasterToGameServer function")]
    public static Action<string, int> onPrepareToChangeFromMasterToGameServer; // string: IP, int: Port.

    [Tooltip("Delegate to the OnChangeFromMasterToGameServer function")]
    public static Action<string, int> onChangeFromMasterToGameServer; // string: IP, int: Port.

    [Tooltip("Delegate to the OnLeaveGame function")]
    public static Action onLeaveGame;

    [Tooltip("Delegate to the OnPrepareToChangeFromGameToMasterServer function")]
    public static Action<string, int> onPrepareToChangeFromGameToMasterServer; // string: IP, int: Port.

    [Tooltip("Delegate to the OnChangeFromGameToMasterServer function")]
    public static Action<string, int> onChangeFromGameToMasterServer; // string: IP, int: Port.

    [Tooltip("Delegate to the OnReturnToMasterServer function")]
    public static Action<UserData> onReturnToMasterServer;

    [Tooltip("Delegate to the OnServerClosed function")]
    public static Action onServerClosed;


    [Header("Moment Delegates")]
    [Tooltip("Delegate to the OnGameFound function")]
    public static Action<GameFoundData> onGameFound;

    [Tooltip("Delegate to the OnGameStart function")]
    public static Action onGameStart;

    [Tooltip("Delegate to the OnGameEnd function")]
    public static Action<GameEndData> onGameEnd;

    [Tooltip("Delegate to the OnGameSenderTick function")]
    public static Action onGameSenderTick;

    [Tooltip("Delegate to the OnGameUpdate function")]
    public static Action<UpdateData> onGameReceiveUpdate;

    [Tooltip("Delegate to the OnAnotherPlayerLeaveGame function")]
    public static Action<PlayerData> onAnotherPlayerLeaveGame;


    [Header("Control Delegates")]
    [Tooltip("Delegate to the OnRTT function")]
    public static Action<long> onRTT; // long: milliseconds that RTT lasted.
    #endregion
}