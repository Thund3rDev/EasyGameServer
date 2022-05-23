using System.IO;
using System.Xml;
using UnityEngine;

/// <summary>
/// Class MasterServer, that controls the Master Server side.
/// </summary>
public class MasterServer : MonoBehaviour
{
    #region Variables
    [Header("Control")]
    [Tooltip("Bool that indicates if the server has started or not")]
    private bool serverStarted = false;

    [Tooltip("Reference to the server socket manager")]
    private MasterServerSocketManager serverSocketManager = null;
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
            Log.instance.WriteWarningLog("Easy Game Server already started.", EasyGameServerControl.EnumLogDebugLevel.Minimal);
            return;
        }

        // Start the server.
        serverStarted = true;

        // Start the server log.
        Log.instance.StartLog();

        // Read Server config data.
        ReadConfigData();

        // Initialize Server Games Manager.
        ServerGamesManager.instance.InitializeServerGamesManager();

        // Call the onMasterServerStart delegate.
        MasterServerDelegates.onMasterServerStart?.Invoke();

        // Create sockets manager.
        serverSocketManager = new MasterServerSocketManager();

        // Start listening for connections.
        serverSocketManager.StartListening();
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
        serverSocketManager.StopListening();

        // Reset the next room number.
        ServerGamesManager.instance.ResetRoomNumber();

        // Call the onMasterServerShutdown delegate.
        MasterServerDelegates.onMasterServerShutdown?.Invoke();

        // Close the Log.
        Log.instance.CloseLog();
    }

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
        EasyGameServerConfig.DEBUG_MODE_CONSOLE = (EasyGameServerControl.EnumLogDebugLevel)int.Parse(node.InnerText);

        node = doc.DocumentElement.SelectSingleNode("//server/debug-mode-file");
        EasyGameServerConfig.DEBUG_MODE_FILE = (EasyGameServerControl.EnumLogDebugLevel)int.Parse(node.InnerText);

        // Get maximum number of connections.
        node = doc.DocumentElement.SelectSingleNode("//server/max-connections");
        EasyGameServerConfig.MAX_CONNECTIONS = int.Parse(node.InnerText);

        // Get maximum number of game servers.
        node = doc.DocumentElement.SelectSingleNode("//server/max-games");
        EasyGameServerConfig.MAX_GAMES = int.Parse(node.InnerText);

        // Get time between round trip times.
        node = doc.DocumentElement.SelectSingleNode("//server/time-between-rtt");
        EasyGameServerConfig.TIME_BETWEEN_RTTS = int.Parse(node.InnerText);

        // Get time to disconnect client if no response.
        node = doc.DocumentElement.SelectSingleNode("//server/disconnect-timeout");
        EasyGameServerConfig.DISCONNECT_TIMEOUT = int.Parse(node.InnerText);


        /// Game Server Data.
        // Get Game Server path to the executable in the file explorer.
        node = doc.DocumentElement.SelectSingleNode("//game-server/path");
        EasyGameServerConfig.GAMESERVER_PATH = node.InnerText;


        /// Networking Data.
        // Get server ip.
        node = doc.DocumentElement.SelectSingleNode("//networking/server-ip");
        EasyGameServerConfig.SERVER_IP = node.InnerText;

        // Get server port.
        node = doc.DocumentElement.SelectSingleNode("//networking/base-port");
        EasyGameServerConfig.SERVER_PORT = int.Parse(node.InnerText);


        /// Games Data.
        node = doc.DocumentElement.SelectSingleNode("//game/players-per-game"); // FUTURE: Make MIN_PLAYERS and MAX_PLAYERS.
        EasyGameServerConfig.PLAYERS_PER_GAME = int.Parse(node.InnerText);
    }
    #endregion
}
