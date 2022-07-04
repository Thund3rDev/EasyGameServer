using UnityEngine;

/// <summary>
/// Struct EasyGameServerConfig, that contains important data and variables read by the config.xml file.
/// </summary>
public struct EasyGameServerConfig
{
    #region Variables
    [Header("Server")]
    [Tooltip("Easy Game Server version")]
    public static readonly string VERSION = "1.0.0";

    [Tooltip("Level of debug of the server console log")]
    public static EasyGameServerControl.EnumLogDebugLevel DEBUG_MODE_CONSOLE;

    [Tooltip("Level of debug of the file log")]
    public static EasyGameServerControl.EnumLogDebugLevel DEBUG_MODE_FILE;

    [Tooltip("Maximum number of connections that the server will handle")]
    public static int MAX_CONNECTIONS;

    [Tooltip("Maximum number of games (game servers) that the server will have at the same time")]
    public static int MAX_GAMES;

    [Tooltip("Time in milliseconds to send a Round Trip Time")]
    public static int TIME_BETWEEN_RTTS;

    [Tooltip("Time in milliseconds to disconnect a client if no response")]
    public static int DISCONNECT_TIMEOUT;


    [Header("GameServer")]
    [Tooltip("Path where the Game Server .exe is on the file explorer")]
    public static string GAMESERVER_PATH;


    [Header("Networking")]
    [Tooltip("IP where the server will be set")]
    public static string SERVER_IP;

    [Tooltip("Port where the server will be set")]
    public static int SERVER_PORT;

    [Tooltip("Maximum number of tries to connect to the server")]
    public static int CONNECTION_TRIES;


    [Header("Game")]
    [Tooltip("Number of players to start a game")]
    public static int PLAYERS_PER_GAME;

    [Tooltip("Number of calculations per second by the game server for a game")]
    public static int CALCULATIONS_PER_SECOND;
    #endregion
}