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

    [Tooltip("Reference to the Socket Receiver")]
    [SerializeField]
    private EGS_SE_SocketReceiver egs_se_SocketReceiver = null;

    [Tooltip("Reference to the Socket Sender")]
    [SerializeField]
    private EGS_CL_SocketSender egs_cl_SocketSender = null;
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartServer, that initializes the server and loads the data.
    /// </summary>
    public void StartServer()
    {
        // Check if server already started.
        if (serverStarted)
        {
            egs_Log.LogWarning("Easy Game Server already started.");
            return;
        }

        // Start the server.
        serverStarted = true;
        egs_Log.StartLog(serverVersion);
        egs_se_SocketReceiver.StartServer();

        // Test socket connection
        egs_cl_SocketSender.StartClient();

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
