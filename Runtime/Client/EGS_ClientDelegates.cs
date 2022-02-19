using System;
using UnityEngine;

public static class EGS_ClientDelegates
{
    #region Variables
    [Header("Delegates")]
    [Tooltip("Delegate to the OnMessageReceive function")]
    public static Action<EGS_Message> onMessageReceive;
    #endregion
}
