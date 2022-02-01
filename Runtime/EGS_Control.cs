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

    public static EGS_Control instance = null;
    public EGS_Type egs_type = EGS_Type.Empty;

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
