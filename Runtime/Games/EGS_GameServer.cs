using System;
using System.IO;
using System.Net.Sockets;
using System.Xml;
using UnityEngine;

/// <summary>
/// Class EGS_GameServer, that controls the game server side.
/// </summary>
public class EGS_GameServer : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_GameServer gameServer_instance;

    [Tooltip("Struct that contains the server data")]
    public EGS_ServerData serverData;

    [Tooltip("Controller for game server sockets")]
    public EGS_GS_Sockets gameServerSocketsController = null;

    public bool connectedToServer;

    public EGS_GameServerData.State gameServerState;

    public int gameServerID = -1;
    public EGS_GameServerStartData startData;

    public TMPro.TextMeshProUGUI test_text;

    public EGS_Game thisGame;

    public readonly int PLAYERS_PER_GAME = 2;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (gameServer_instance == null)
        {
            gameServer_instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        ReadArguments();
        ConnectToMasterServer();
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method ConnectToMasterServer, that tries to connect to the server.
    /// </summary>
    private void ConnectToMasterServer()
    {
        // Create sockets manager.
        gameServerSocketsController = new EGS_GS_Sockets();

        // Connect to the server.
        gameServerSocketsController.ConnectToMasterServer();
    }

    /// <summary>
    /// Method DisconnectFromMasterServer, that will stop sending messages and listening.
    /// </summary>
    private void DisconnectFromMasterServer()
    {
        // Stop listening on the sockets.
        gameServerSocketsController.Disconnect();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method ReadArguments, to load the received arguments.
    /// </summary>
    private void ReadArguments()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        string[] realArguments = arguments[1].Split('#');
        serverData.version = realArguments[0];
        serverData.serverIP = realArguments[1];
        serverData.serverPort = int.Parse(realArguments[2]);
        gameServerID = int.Parse(realArguments[3]);
        startData = JsonUtility.FromJson<EGS_GameServerStartData>(realArguments[4]);
    }
    #endregion
}
