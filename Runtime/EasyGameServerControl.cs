using UnityEngine;

/// <summary>
/// Class EasyGameServerControl, to manage common control variables.
/// </summary>
public class EasyGameServerControl : MonoBehaviour
{
    #region Enums
    /// <summary>
    /// Enum EnumInstanceType, to know the type of the instance running.
    /// MasterServer, GameServer or Client.
    /// </summary>
    public enum EnumInstanceType
    {
        Empty = -1,
        MasterServer = 0,
        GameServer = 1,
        Client = 2
    }

    /// <summary>
    /// Enum EnumLogDebugLevel, to establish the level of debug that the instance will use.
    /// No_Debug, Minimal, Useful, Extended or Complete.
    /// </summary>
    public enum EnumLogDebugLevel
    {
        No_Debug = -1,
        Minimal = 0,
        Useful = 1,
        Extended = 2,
        Complete = 3
    }
    #endregion

    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EasyGameServerControl instance = null;


    [Header("Control")]
    [Tooltip("EasyGameServer instance type")]
    public EnumInstanceType instanceType = EnumInstanceType.Empty;
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, called on script load.
    /// </summary>
    private void Awake()
    {
        // Instantiate the singleton.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
            Destroy(this.gameObject);
    }
    #endregion
}