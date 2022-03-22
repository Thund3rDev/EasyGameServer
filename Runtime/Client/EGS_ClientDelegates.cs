using System;
using UnityEngine;

public static class EGS_ClientDelegates
{
    #region Variables
    [Header("General Delegates")]
    [Tooltip("Delegate to the OnMessageReceive function")]
    public static Action<EGS_Message> onMessageReceive;

    [Tooltip("Delegate to the OnUserCreate function")]
    public static Action<EGS_User> onUserCreate;


    [Header("Connect Delegates")]
    [Tooltip("Delegate to the OnConnect function")]
    public static Action<EGS_Control.EGS_Type> onConnect;

    [Tooltip("Delegate to the OnJoinMasterServer function")]
    public static Action<EGS_User> onJoinMasterServer;

    [Tooltip("Delegate to the OnJoinGameServer function")]
    public static Action onJoinGameServer;

    [Tooltip("Delegate to the OnDisconnect function")]
    public static Action onDisconnect;

    [Tooltip("Delegate to the OnPrepareToChangeFromMasterToGameServer function")]
    public static Action<string, int> onPrepareToChangeFromMasterToGameServer;

    [Tooltip("Delegate to the OnChangeFromMasterToGameServer function")]
    public static Action<string, int> onChangeFromMasterToGameServer;


    [Header("Moment Delegates")]
    [Tooltip("Delegate to the OnGameFound function")]
    public static Action<EGS_GameFoundData> onGameFound;

    [Tooltip("Delegate to the OnGameStart function")]
    public static Action onGameStart;

    [Tooltip("Delegate to the OnGameEnd function")]
    public static Action<EGS_Message> onGameEnd;

    [Tooltip("Delegate to the onGameUpdate function")]
    public static Action<EGS_Message> onGameUpdate;


    [Header("Control Delegates")]
    [Tooltip("Delegate to the OnRTT function")]
    public static Action<long> onRTT;
    #endregion
}