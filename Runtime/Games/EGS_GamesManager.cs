using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System;

public class EGS_GamesManager : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_GamesManager gm_instance;

    [Tooltip("Máximum number of games")]
    public readonly int MAX_GAMES = 5;

    [Tooltip("Number of players per game")]
    public readonly int PLAYERS_PER_GAME = 2;

    [Header("Games")]
    public Dictionary<int, List<EGS_Player>> playersInRooms = new Dictionary<int, List<EGS_Player>>();
    [Tooltip("ConcurrentQueue that stores players that are searching a game")]
    public ConcurrentQueue<EGS_Player> searchingGame_players = new ConcurrentQueue<EGS_Player>();

    [Tooltip("Array with the Game Servers")]
    public EGS_GameServerData[] gameServers = new EGS_GameServerData[10];

    [Header("Control")]
    [Tooltip("Integer that assigns the room number for the next room")]
    private int nextRoom = 0;

    [Tooltip("Mutex that controls concurrency for create and delete games")]
    public Mutex gamesLock = new Mutex();

    [Header("References")]
    [Tooltip("Reference to the log")]
    [SerializeField]
    private EGS_Log egs_Log;
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
        }
        else
            Destroy(this.gameObject);

    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method CreateGame, that will create a new instance of a game and assign all data.
    /// </summary>
    /// <param name="socketController">Instance of the socket controller</param>
    /// <param name="playersToGame">List of players that will play that game</param>
    /// <returns>Room number</returns>
    public int CreateGame(EGS_SE_SocketController socketController, List<EGS_Player> playersToGame)
    {
        // Get the room number.
        int room = Interlocked.Increment(ref nextRoom);

        string logString = "Created game with room " + room + ". Players: ";

        // Assign the room number to the players.
        foreach (EGS_Player p in playersToGame)
        {
            p.SetRoom(room);
            logString += p.GetUser().GetUsername() + ", ";
        }

        // Add the players to the dictionary of rooms.
        playersInRooms.Add(room, playersToGame);

        // Launch the GameServer.
        LaunchGameServer(room, playersToGame);

        egs_Log.Log(logString);

        // Return the room number.
        return room;
    }

    public void FinishedGame(int room)
    {
        // Remove the room number for players.
        /*foreach (EGS_Player p in gameCD.Game.GetPlayers())
            p.SetRoom(-1);*/

        egs_Log.Log("Stopped and closed game with room " + room);
    }

    public void QuitPlayerFromGame(EGS_Player leftPlayer)
    {
        int room = leftPlayer.GetRoom();

        egs_Log.Log("Player " + leftPlayer.GetUser().GetUsername() + " left the game on room " + room + ".");

        leftPlayer.SetRoom(-1);
        leftPlayer.SetIngameID(-1);
    }

    #region Private Methods
    private void LaunchGameServer(int room, List<EGS_Player> playersToGame)
    {
        int gameServerID = -1;
        bool serverAvailable = false;
        int index = 0;

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
            EGS_GameServerStartData startData = new EGS_GameServerStartData(room);

            foreach (EGS_Player player in playersToGame)
            {
                EGS_UserToGame userToGame = new EGS_UserToGame(player.GetUser(), player.GetIngameID());
                startData.GetUsersToGame().Add(userToGame);
            }

            string arguments = EGS_ServerManager.serverData.version + "#" + EGS_ServerManager.serverData.serverIP + "#" + EGS_ServerManager.serverData.serverPort + "#" + gameServerID;
            string jsonString = JsonUtility.ToJson(startData);
            arguments += "#" + jsonString;

            gameServers[gameServerID] = new EGS_GameServerData(gameServerID, room);

            try
            {
                gameServers[gameServerID].Process = new Process();
                gameServers[gameServerID].Process.StartInfo.FileName = "C:\\Users\\Samue\\Desktop\\URJC\\TFG\\Builds\\Game Server\\Easy Game Server.exe";
                gameServers[gameServerID].Process.StartInfo.Arguments = arguments;
                gameServers[gameServerID].Process.Start();

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
