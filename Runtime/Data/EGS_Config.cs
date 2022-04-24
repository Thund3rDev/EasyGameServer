using UnityEngine;

/// <summary>
/// Struct EGS_Config, that contains important data and variables.
/// </summary>
public struct EGS_Config
{
    [Tooltip("Easy Game Server version")]
    public static readonly string version = "0.1.0";

    [Tooltip("Int that indicates the level of debug")]
    public static int DEBUG_MODE = 2; /// -1: No debug | 0: Release debug | 1: Minimal debug | 2: Some useful debugs | 3: Complete debug

    [Tooltip("IP where the server will be set")]
    public static string serverIP;

    [Tooltip("Port where the server will be set")]
    public static int serverPort;

    [Tooltip("Maximum number of connections that the server will handle")]
    public static int MAX_CONNECTIONS;

    [Tooltip("Maximum number of games (game servers) that the server will have at the same time")]
    public static int MAX_GAMES;

    [Tooltip("Maximum number of tries to connect to the server")]
    public static int CONNECTION_TRIES;

    [Tooltip("Time in milliseconds to send a Round Trip Time")]
    public static int TIME_BETWEEN_RTTS;

    [Tooltip("Time in milliseconds to disconnect a client if no response")]
    public static int DISCONNECT_TIMEOUT;

    [Tooltip("Path where the Game Server .exe is on the file explorer")]
    public static string GAMESERVER_PATH;

    [Tooltip("Number of players to start a game")]
    public static int PLAYERS_PER_GAME;

    [Tooltip("Number of calculations per second by the game server for a game")]
    public static int CALCULATIONS_PER_SECOND;
}
