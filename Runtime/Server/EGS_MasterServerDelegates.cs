using System;
using UnityEngine;

public class EGS_MasterServerDelegates
{
    #region Variables
    [Header("General Delegates")]
    [Tooltip("Delegate to the OnMessageReceive function")]
    public static Action<EGS_Message> onMessageReceive;


    [Header("Master Server Control Delegates")]
    [Tooltip("Delegate to the OnMasterServerStart function")]
    public static Action onMasterServerStart;

    [Tooltip("Delegate to the OnMasterServerShutdown function")]
    public static Action onMasterServerShutdown;

    [Header("User Control Delegates")]
    [Tooltip("Delegate to the OnUserJoinServer function")]
    public static Action<EGS_User> onUserJoinServer;

    [Tooltip("Delegate to the OnUserRegister function")]
    public static Action<EGS_User> onUserRegister;

    [Tooltip("Delegate to the OnUserConnect function")]
    public static Action<EGS_User> onUserConnect;

    [Tooltip("Delegate to the OnUserDelete function")]
    public static Action<EGS_User> onUserDelete;

    [Tooltip("Delegate to the OnUserDisconnect function")]
    public static Action<EGS_User> onUserDisconnect;


    [Header("User Moment Delegates")]
    [Tooltip("Delegate to the OnUserJoinQueue function")]
    public static Action<EGS_User> onUserJoinQueue;

    [Tooltip("Delegate to the OnUserLeaveQueue function")]
    public static Action<EGS_User> onUserLeaveQueue;

    [Tooltip("Delegate to the OnUserDisconnectToGameServer function")]
    public static Action<EGS_User> onUserDisconnectToGameServer;

    [Tooltip("Delegate to the OnUserLeaveGame function")]
    public static Action<EGS_User> onUserLeaveGame;


    [Header("Game Server Control Delegates")]
    [Tooltip("Delegate to the OnGameServerCreated function")]
    public static Action<int> onGameServerCreated;

    [Tooltip("Delegate to the OnGameServerClosed function")]
    public static Action<int> onGameServerClosed;


    [Header("Game Control Delegates")]
    [Tooltip("Delegate to the OnGameFound function")]
    public static Action<EGS_GameFoundData> onGameFound;

    [Tooltip("Delegate to the OnGameStart function")]
    public static Action<int, EGS_Message> onGameStart;

    [Tooltip("Delegate to the OnGameEnd function")]
    public static Action<int, EGS_Message> onGameEnd;


    [Header("Control Delegates")]
    [Tooltip("Delegate to the OnClientRTT function")]
    public static Action<int, long> onClientRTT;

    [Tooltip("Delegate to the OnClientRTT function")]
    public static Action<int, long> onGameServerRTT;
    #endregion
}
