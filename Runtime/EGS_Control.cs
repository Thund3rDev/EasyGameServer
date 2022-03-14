using UnityEngine;

public class EGS_Control : MonoBehaviour
{
    public enum EGS_Type
    {
        Empty = -1,
        MasterServer = 0,
        GameServer = 1,
        Client = 2
    }

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
