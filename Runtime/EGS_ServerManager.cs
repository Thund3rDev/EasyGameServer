using System.IO;
using System.Xml;
using UnityEngine;

/// <summary>
/// Class EGS_ServerManager, that controls the core of EGS.
/// </summary>
public class EGS_ServerManager : MonoBehaviour
{
    #region Variables
    [Header("Control")]
    [Tooltip("Bool that indicates if the server has started or not")]
    private bool serverStarted = false;

    [Tooltip("Reference to the server socket manager")]
    private EGS_SE_Sockets socketsController = null;
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
            EGS_Log.instance.LogWarning("Easy Game Server already started.", EGS_Control.EGS_DebugLevel.Minimal);
            return;
        }

        // Start the server.
        serverStarted = true;

        // Start the server log.
        EGS_Log.instance.StartLog();

        // Read Server config data.
        ReadConfigData();

        // Initialize Server Games Manager.
        EGS_ServerGamesManager.instance.InitializeServerGamesManager();

        // TODO: ReadUsersData.


        // Call the onMasterServerStart delegate.
        EGS_MasterServerDelegates.onMasterServerStart?.Invoke();

        // Create sockets manager.
        socketsController = new EGS_SE_Sockets();

        // Start listening for connections.
        socketsController.StartListening();
    }

    /// <summary>
    /// Method ShutdownServer, that closes the server.
    /// </summary>
    public void ShutdownServer()
    {
        // If server hasn't started, return.
        if (!serverStarted)
            return;

        // Stop the server.
        serverStarted = false;

        // Stop listening on the socket.
        socketsController.StopListening();

        // TODO:  Disconnect players.
        // TODO:  Save all data.

        // Call the onMasterServerShutdown delegate.
        EGS_MasterServerDelegates.onMasterServerShutdown?.Invoke();

        // Close the Log.
        EGS_Log.instance.CloseLog();
    }

    #region Private Methods
    /// <summary>
    /// Method ReadServerData, to load all server config data.
    /// </summary>
    private void ReadConfigData()
    {
        // Read server config data.
        string configXMLPath = "Packages/com.thund3r.easy_game_server/config.xml";
        if (!File.Exists("Packages/com.thund3r.easy_game_server/config.xml"))
            configXMLPath = "./config.xml";

        XmlDocument doc = new XmlDocument();
        doc.Load(configXMLPath);

        XmlNode node;

        /// Server Data.
        // Get debug mode.
        node = doc.DocumentElement.SelectSingleNode("//server/debug-mode-console");
        EGS_Config.DEBUG_MODE_CONSOLE = (EGS_Control.EGS_DebugLevel)int.Parse(node.InnerText);

        node = doc.DocumentElement.SelectSingleNode("//server/debug-mode-file");
        EGS_Config.DEBUG_MODE_FILE = (EGS_Control.EGS_DebugLevel)int.Parse(node.InnerText);

        // Get maximum number of connections.
        node = doc.DocumentElement.SelectSingleNode("//server/max-connections");
        EGS_Config.MAX_CONNECTIONS = int.Parse(node.InnerText);

        // Get maximum number of game servers.
        node = doc.DocumentElement.SelectSingleNode("//server/max-games");
        EGS_Config.MAX_GAMES = int.Parse(node.InnerText);

        // Get time between round trip times.
        node = doc.DocumentElement.SelectSingleNode("//server/time-between-rtt");
        EGS_Config.TIME_BETWEEN_RTTS = int.Parse(node.InnerText);

        // Get time to disconnect client if no response.
        node = doc.DocumentElement.SelectSingleNode("//server/disconnect-timeout");
        EGS_Config.DISCONNECT_TIMEOUT = int.Parse(node.InnerText);


        /// Game Server Data.
        // Get Game Server path to the executable in the file explorer.
        node = doc.DocumentElement.SelectSingleNode("//game-server/path");
        EGS_Config.GAMESERVER_PATH = node.InnerText;


        /// Networking Data.
        // Get server ip.
        node = doc.DocumentElement.SelectSingleNode("//networking/server-ip");
        EGS_Config.serverIP = node.InnerText;

        // Get server port.
        node = doc.DocumentElement.SelectSingleNode("//networking/base-port");
        EGS_Config.serverPort = int.Parse(node.InnerText);


        /// Games Data.
        node = doc.DocumentElement.SelectSingleNode("//game/players-per-game"); // TODO: Make MIN_PLAYERS and MAX_PLAYERS.
        EGS_Config.PLAYERS_PER_GAME = int.Parse(node.InnerText);
    }
    #endregion
    #endregion
}
