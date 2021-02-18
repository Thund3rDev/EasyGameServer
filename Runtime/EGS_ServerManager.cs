using System;
using System.Xml;
using System.Threading.Tasks;
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

    [Header("Tasks")]
    [Tooltip("Task that manages the server listening")]
    private Task listeningTask;

    [Header("References")]
    [Tooltip("Reference to the Log")]
    [SerializeField]
    private EGS_Log egs_Log = null;

    [Tooltip("Reference to the Socket Listener")]
    [SerializeField]
    private EGS_SE_SocketListener egs_SE_SocketListener = null;

    [Tooltip("Reference to the Socket Sender")]
    [SerializeField]
    private EGS_CL_SocketSender egs_CL_SocketSender = null;
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

        // Start listening for connections.
        Action listeningAction = new Action(StartListening);
        listeningTask = Task.Factory.StartNew(listeningAction);

        // Test socket connection.
        egs_CL_SocketSender.StartClient(serverData.serverPort);
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

        // Close the socket.
        egs_SE_SocketListener.StopListening();

        // Save all data.
        // Disconnect players.
        egs_Log.CloseLog();
    }

    #region Private Methods

    /// <summary>
    /// Method StartListening, to start listening sockets.
    /// </summary>
    private void StartListening()
    {
        egs_SE_SocketListener.StartListening(serverData.serverIP, serverData.serverPort);
    }

    /// <summary>
    /// Method ReadServerData, to load all server config data.
    /// </summary>
    private void ReadServerData()
    {
        // Read server config data.
        string configXMLPath = "Packages/com.thund3rdev.easy_game_server/config.xml";
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
