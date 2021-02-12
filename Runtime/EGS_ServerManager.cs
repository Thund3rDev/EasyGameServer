using UnityEngine;

/// <summary>
/// Class EGS_ServerManager, that controls the core of EGS
/// </summary>
public class EGS_ServerManager : MonoBehaviour
{
    #region Variables
    [Tooltip("Bool that indicates if the server has started or not")]
    private bool serverStarted = false;
    #endregion

    /// <summary>
    /// Method StartServer, that initializes the server and loads the data.
    /// </summary>
    public void StartServer()
    {
        if (serverStarted)
        {
            //Log "Server already started"
            return;
        }

        // Start the server
        serverStarted = true;

        // Log Start
        // Read all data
    }

    /// <summary>
    /// Method Shutdown, that closes the server.
    /// </summary>
    public void ShutdownServer()
    {
        // Save all data
        // Disconnect players
        // Log Shutdown
    }
}
