using System;
using UnityEngine;

public static class EGS_GameServerDelegates
{
    #region Variables
    [Header("General Delegates")]
    [Tooltip("Delegate to the OnMessageReceive function")]
    public static Action<EGS_Message> onMessageReceive;


    /*//[Tooltip("Delegate to the OnJoinMasterServer function")]
    //public static Action<EGS_Message> onJoinMasterServer;

    //[Tooltip("Delegate to the OnGameFound function")]
    //public static Action<EGS_GameFoundData> onGameFound;*/
    [Header("Moment Delegates")]
    [Tooltip("Delegate to the OnGameStart function")]
    public static Action onGameStart;


    [Header("Control Delegates")]
    [Tooltip("Delegate to the OnRTT function")]
    public static Action<long> onRTT;
    #endregion
}
