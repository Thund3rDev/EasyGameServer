using UnityEngine;

/// <summary>
/// Class EGS_Control, to manage common control variables.
/// </summary>
public class EGS_Control : MonoBehaviour
{
    #region Enums
    /// <summary>
    /// Enum EGS_Type, to know the type of the instance running.
    /// MasterServer, GameServer or Client.
    /// </summary>
    public enum EGS_Type
    {
        Empty = -1,
        MasterServer = 0,
        GameServer = 1,
        Client = 2
    }

    /// <summary>
    /// Enum EGS_DebugLevel, to establish the level of debug that the instance will use.
    /// No_Debug, Minimal, Useful, Extended or Complete.
    /// </summary>
    public enum EGS_DebugLevel
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
    public static EGS_Control instance = null;


    [Header("Control")]
    [Tooltip("EasyGameServer type")]
    public EGS_Type egs_type = EGS_Type.Empty;
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
