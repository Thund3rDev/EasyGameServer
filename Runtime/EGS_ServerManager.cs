using System.IO;
using System.Xml;
using UnityEngine;

/// <summary>
/// Class EGS_ServerManager, that controls the core of EGS.
/// </summary>
public class EGS_ServerManager : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Struct that contains the server data")]
    [HideInInspector]
    private EGS_ServerData serverData;

    [Tooltip("Bool that indicates if the server has started or not")]
    private bool serverStarted = false;

    [Tooltip("Int that indicates the level of debug")]
    public static readonly int DEBUG_MODE = 1; // 0: No debug | 1: Minimal debug | 2: Complete debug

    [Header("References")]
    [Tooltip("Reference to the Log")]
    [SerializeField]
    private EGS_Log egs_Log = null;

    [Tooltip("Reference to the server socket manager")]
    private EGS_SE_Sockets egs_se_sockets = null;
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

        // Start the server log.
        egs_Log.StartLog();

        // Read Server config data.
        ReadServerData();

        // Log that server started.
        egs_Log.Log("Started <color=green>EasyGameServer</color> with version <color=orange>" + serverData.version + "</color>.");

        /// Read all data.

        // Create sockets manager
        egs_se_sockets = new EGS_SE_Sockets(egs_Log, serverData.serverIP, serverData.serverPort);
        // Start listening for connections
        egs_se_sockets.StartListening();
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

        // Stop listening on the socket.
        egs_se_sockets.StopListening();

        // Save all data.
        // Disconnect players.
        egs_Log.CloseLog();
    }

    #region Private Methods
    /// <summary>
    /// Method ReadServerData, to load all server config data.
    /// </summary>
    private void ReadServerData()
    {
        // Read server config data.
        string configXMLPath = "Packages/com.thund3r.easy_game_server/config.xml";
        if (!File.Exists("Packages/com.thund3r.easy_game_server/config.xml"))
            configXMLPath = "./config.xml";

        XmlDocument doc = new XmlDocument();
        doc.Load(configXMLPath);

        XmlNode node;

        // Get server version.
        node = doc.DocumentElement.SelectSingleNode("//version");
        serverData.version = node.InnerText;

        // Get server ip.
        node = doc.DocumentElement.SelectSingleNode("//server-ip");
        serverData.serverIP = node.InnerText;

        // Get server port.
        node = doc.DocumentElement.SelectSingleNode("//port");
        serverData.serverPort = int.Parse(node.InnerText);
    }
    #endregion
    #endregion
}
