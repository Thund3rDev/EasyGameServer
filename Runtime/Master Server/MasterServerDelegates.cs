using System;
using UnityEngine;

/// <summary>
/// Class MasterServerDelegates, that contains the delegates that can be used on the master server side on specific moments.
/// </summary>
public class MasterServerDelegates
{
    #region Delegates
    [Header("General Delegates")]
    [Tooltip("Delegate to the OnMessageReceive function")]
    public static Action<NetworkMessage> onMessageReceive;


    [Header("Master Server Control Delegates")]
    [Tooltip("Delegate to the OnMasterServerStart function")]
    public static Action onMasterServerStart;

    [Tooltip("Delegate to the OnMasterServerShutdown function")]
    public static Action onMasterServerShutdown;


    [Header("User Control Delegates")]
    [Tooltip("Delegate to the OnUserJoinServer function")]
    public static Action<UserData, bool> onUserJoinServer; // bool: is user returning from a game.

    [Tooltip("Delegate to the OnUserRegister function")]
    public static Action<UserData> onUserRegister;

    [Tooltip("Delegate to the OnUserConnect function")]
    public static Action<UserData> onUserConnect;

    [Tooltip("Delegate to the OnUserDisconnect function")]
    public static Action<UserData> onUserDisconnect;

    [Tooltip("Delegate to the OnUserDelete function")]
    public static Action<UserData> onUserDelete;


    [Header("User Moment Delegates")]
    [Tooltip("Delegate to the OnUserJoinQueue function")]
    public static Action<UserData> onUserJoinQueue;

    [Tooltip("Delegate to the OnUserLeaveQueue function")]
    public static Action<UserData> onUserLeaveQueue;

    [Tooltip("Delegate to the OnUserDisconnectToGameServer function")]
    public static Action<UserData> onUserDisconnectToGameServer;

    [Tooltip("Delegate to the OnUserLeaveGame function")]
    public static Action<UserData> onUserLeaveGame;


    [Header("Game Server Control Delegates")]
    [Tooltip("Delegate to the OnGameServerCreated function")]
    public static Action<int> onGameServerCreated; // int: Game Server ID.

    [Tooltip("Delegate to the OnGameServerReady function")]
    public static Action<int> onGameServerReady; // int: Game Server ID.

    [Tooltip("Delegate to the OnGameServerClosed function")]
    public static Action<int> onGameServerClosed; // int: Game Server ID.


    [Header("Game Control Delegates")]
    [Tooltip("Delegate to the OnGameFound function")]
    public static Action<GameFoundData> onGameFound;

    [Tooltip("Delegate to the OnGameStart function")]
    public static Action<UpdateData> onGameStart;

    [Tooltip("Delegate to the OnGameEnd function")]
    public static Action<GameEndData> onGameEnd;


    [Header("Control Delegates")]
    [Tooltip("Delegate to the OnClientRTT function")]
    public static Action<int, long> onClientRTT; // int: Client ID, long: milliseconds.

    [Tooltip("Delegate to the OnClientRTT function")]
    public static Action<int, long> onGameServerRTT; // int: Game Server ID, long: milliseconds.
    #endregion
}
