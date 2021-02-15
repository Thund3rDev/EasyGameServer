using UnityEngine;

/// <summary>
/// Class EGS_ServerManager, that controls the core of EGS.
/// </summary>
public class EGS_ServerManager : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("String that contains the server version")]
    [HideInInspector]
    public string serverVersion = "0.0.1";

    [Tooltip("Bool that indicates if the server has started or not")]
    private bool serverStarted = false;

    [Header("References")]
    [Tooltip("Reference to the Log")]
    [SerializeField]
    private EGS_Log egs_Log = null;
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartServer, that initializes the server and loads the data.
    /// </summary>
    public void StartServer()
    {
        if (serverStarted)
        {
            // Should be in yellow.
            egs_Log.Log("Easy Game Server already started");
            return;
        }

        // Start the server.
        serverStarted = true;

        egs_Log.StartLog(serverVersion);
        // Read all data.
    }

    /// <summary>
    /// Method Shutdown, that closes the server.
    /// </summary>
    public void ShutdownServer()
    {
        // If server hasn't started, return.
        if (!serverStarted)
            return;

        // Stop the server.
        serverStarted = false;

        // Save all data.
        // Disconnect players.
        egs_Log.CloseLog();
    }
    #endregion
}
