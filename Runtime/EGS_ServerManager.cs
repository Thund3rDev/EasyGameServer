using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public static EGS_ServerData serverData;

    [Tooltip("Bool that indicates if the server has started or not")]
    private bool serverStarted = false;

    [Tooltip("Int that indicates the level of debug")]
    public static readonly int DEBUG_MODE = 2; // 0: No debug | 1: Minimal debug | 2: Some useful debugs | 3: Complete debug

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

        // Create sockets manager.
        egs_se_sockets = new EGS_SE_Sockets(egs_Log, serverData.serverIP, serverData.serverPort);
        // Start listening for connections.
        egs_se_sockets.StartListening();

        /// TEST
        /*EGS_User user1 = new EGS_User();
        user1.SetUserID(0);
        user1.SetUsername("user1");

        EGS_User user2 = new EGS_User();
        user2.SetUserID(1);
        user2.SetUsername("user2");

        EGS_GameServerStartData startData = new EGS_GameServerStartData(0);
        startData.GetUsersToGame().Add(user1);
        startData.GetUsersToGame().Add(user2);

        LaunchGameServer(0, startData);*/
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

    /// TEST
    public void LaunchGameServer(int gameServerID, EGS_GameServerStartData startData)
    {
        string arguments = serverData.version + "#" + serverData.serverIP + "#" + serverData.serverPort + "#" + gameServerID;
        string jsonString = JsonUtility.ToJson(startData);
        arguments += "#" + jsonString;

        try
        {
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "C:\\Users\\Samue\\Desktop\\URJC\\TFG\\Builds\\Game Server\\Easy Game Server.exe";
            myProcess.StartInfo.Arguments = arguments;
            myProcess.Start();
        }
        catch (Exception e)
        {
            egs_Log.LogError(e.ToString());
        }
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
