using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System;

/// <summary>
/// Class EGS_ServerGamesManager, that creates and manages the new games.
/// </summary>
public class EGS_ServerGamesManager : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_ServerGamesManager gm_instance;

    [Header("Fixed Variables")]
    [Tooltip("M�ximum number of games")]
    public readonly int MAX_GAMES = 5;

    [Tooltip("Number of players per game")]
    public readonly int PLAYERS_PER_GAME = 2;

    [Header("Games")]
    [Tooltip("Dictionary that stores users by their room")]
    public Dictionary<int, List<EGS_User>> usersInRooms = new Dictionary<int, List<EGS_User>>();

    [Tooltip("ConcurrentQueue that stores players that are searching a game")]
    public ConcurrentQueue<EGS_PlayerToGame> searchingGame_players = new ConcurrentQueue<EGS_PlayerToGame>();

    [Tooltip("Array with the Game Servers")]
    public EGS_GameServerData[] gameServers;

    [Header("Control")]
    [Tooltip("Integer that assigns the room number for the next room")]
    private int nextRoom = 0;

    [Header("Sync")]
    [Tooltip("Mutex that controls concurrency for create and delete games")]
    public Mutex gamesLock = new Mutex();

    [Header("References")]
    [Tooltip("Reference to the log")]
    [SerializeField] private EGS_Log egs_Log;
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, called on script load.
    /// </summary>
    private void Awake()
    {
        if (gm_instance == null)
        {
            gm_instance = this;
            DontDestroyOnLoad(this.gameObject);

            gameServers = new EGS_GameServerData[MAX_GAMES];
        }
        else
            Destroy(this.gameObject);

    }
    #endregion

    #region Class Methods
    #region Public Methods
    /// <summary>
    /// Method CreateGame, that will create a new instance of a game and assign all data.
    /// </summary>
    /// <param name="playersToGame">List of players that will play that game</param>
    /// <returns>Room number</returns>
    public int CreateGame(List<EGS_PlayerToGame> playersToGame)
    {
        // Get the room number.
        int room = Interlocked.Increment(ref nextRoom);

        string logString = "Created game with room " + room + ". Players: ";

        // Assign the room number to the players.
        foreach (EGS_PlayerToGame playerToGame in playersToGame)
        {
            playerToGame.GetUser().SetRoom(room);
            logString += playerToGame.GetUser().GetUsername() + ", ";
        }

        // Launch the GameServer.
        LaunchGameServer(room, playersToGame);

        // Log the information.
        egs_Log.Log(logString);

        // Return the room number.
        return room;
    }

    /// <summary>
    /// Method FinishedGame, executed when a game ends.
    /// </summary>
    /// <param name="room">Room number of that game</param>
    public void FinishedGame(int room)
    {
        // TODO: Make this work.
        // Remove the room number for players.
        /*foreach (EGS_Player p in gameCD.Game.GetPlayers())
            p.SetRoom(-1);*/

        egs_Log.Log("Stopped and closed game with room " + room);
    }

    /// <summary>
    /// Method QuitPlayerFromGame, that disconnects a player from a game. 
    /// </summary>
    /// <param name="leftUser">Player's user</param>
    public void QuitPlayerFromGame(EGS_User leftUser)
    {
        // TODO: Make this work. Think on a EGS_PlayerToGame for the parameter.
        int room = leftUser.GetRoom();

        egs_Log.Log("Player " + leftUser.GetUsername() + " left the game on room " + room + ".");

        leftUser.SetRoom(-1);

        //leftPlayer.SetIngameID(-1);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method LaunchGameServer, that launch an instance of the Game Server with the game parameters.
    /// </summary>
    /// <param name="room">Room number</param>
    /// <param name="playersToGame">List of players to play that game</param>
    private void LaunchGameServer(int room, List<EGS_PlayerToGame> playersToGame)
    {
        int gameServerID = -1;
        bool serverAvailable = false;
        int index = 0;

        // Search for an available server.
        while (!serverAvailable && index < gameServers.Length)
        {
            if (gameServers[index] == null)
            {
                serverAvailable = true;
                gameServerID = index;
            }
            else
            {
                index++;
            }
        }

        // There is a server available.
        if (gameServerID > -1)
        {
            // Prepare the data to be sent at launch.
            EGS_GameServerStartData startData = new EGS_GameServerStartData(room);

            foreach (EGS_PlayerToGame playerToGame in playersToGame)
            {
                startData.GetPlayersToGame().Add(playerToGame);
            }

            // Construct the arguments.
            string arguments = EGS_ServerManager.serverData.version + "#" + EGS_ServerManager.serverData.serverIP + "#" + EGS_ServerManager.serverData.serverPort + "#" + gameServerID;
            string jsonString = JsonUtility.ToJson(startData);
            arguments += "#" + jsonString;

            // Save the GameServer data.
            gameServers[gameServerID] = new EGS_GameServerData(gameServerID, room);

            // Try to launch the GameServer.
            try
            {
                gameServers[gameServerID].SetProcess(new Process());
                gameServers[gameServerID].GetProcess().StartInfo.FileName = "C:\\Users\\Samue\\Desktop\\URJC\\TFG\\Builds\\Game Server\\Easy Game Server.exe";
                gameServers[gameServerID].GetProcess().StartInfo.Arguments = arguments;
                gameServers[gameServerID].GetProcess().Start();

                egs_Log.Log("Launched Game Server with parameters: " + arguments);
            }
            catch (Exception e)
            {
                egs_Log.LogError(e.ToString());
            }
        }
        //TODO: There is NO server available.
        else
        {

        }
    }
    #endregion
    #endregion
}