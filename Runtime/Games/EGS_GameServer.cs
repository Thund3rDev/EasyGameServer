using System;
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
    public EGS_Config serverData;


    [Header("Networking")]
    [Tooltip("Port where the Game Server will be hosted")]
    public int gameServerPort;

    [Tooltip("Controller for game server sockets")]
    public EGS_GS_Sockets gameServerSocketsController = null;

    [Tooltip("Bool that indicates if game server is conected to the master server")]
    public bool connectedToMasterServer;


    [Header("Game Server Data")]
    [Tooltip("Game Server State")]
    public EGS_GameServerData.EGS_GameServerState gameServerState;

    [Tooltip("Game Server ID")]
    public int gameServerID = -1;

    [Tooltip("Instance of the game")]
    public EGS_Game thisGame;

    [Tooltip("Game Server Start Data, that is received on parameters")]
    public EGS_GameServerStartData startData; // TODO: Think if need to store.

    // Test.
    // TODO: Make a Game Server Console with Log and UI.
    public TMPro.TextMeshProUGUI test_text;

    // TODO: Read this from XML.
    public readonly int PLAYERS_PER_GAME = 2;
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, executed on script load.
    /// </summary>
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

    /// <summary>
    /// Method Start, executed before the first frame.
    /// </summary>
    private void Start()
    {
        ReadArguments();
        ConnectToMasterServer();
    }
    #endregion

    #region Class Methods
    #region Public Methods
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
        EGS_Config.serverIP = realArguments[0];
        EGS_Config.serverPort = int.Parse(realArguments[1]);
        gameServerID = int.Parse(realArguments[2]);
        gameServerPort = int.Parse(realArguments[3]);
        startData = JsonUtility.FromJson<EGS_GameServerStartData>(realArguments[4]);
    }
    #endregion
    #endregion
}
