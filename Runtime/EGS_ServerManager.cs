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
    public static EGS_Config serverData;

    [Tooltip("Bool that indicates if the server has started or not")]
    private bool serverStarted = false;

    [Header("References")]
    [Tooltip("Reference to the Log")]
    [SerializeField] private EGS_Log egs_Log = null;

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
            if (EGS_Config.DEBUG_MODE > -1)
                egs_Log.LogWarning("Easy Game Server already started.");
            return;
        }

        // Start the server.
        serverStarted = true;

        // Start the server log.
        egs_Log.StartLog();

        /// Read all data.
        // Read Server config data.
        ReadServerData();
        // Initialize Server Games Manager.
        EGS_ServerGamesManager.gm_instance.InitializeServerGamesManager();

        // TODO: ReadUsersData.

        // Log that server started.
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("Started <color=green>EasyGameServer</color> with version <color=orange>" + EGS_Config.version + "</color>.");

        // Create sockets manager.
        egs_se_sockets = new EGS_SE_Sockets(egs_Log);
        // Start listening for connections.
        egs_se_sockets.StartListening();
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
        egs_se_sockets.StopListening();

        // TODO:  Save all data.
        // TODO:  Disconnect players.
        // TODO:  On Server Shutdown.
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

        /// Server Data.
        // Get debug mode.
        node = doc.DocumentElement.SelectSingleNode("//server/debug-mode");
        EGS_Config.DEBUG_MODE = int.Parse(node.InnerText);

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
