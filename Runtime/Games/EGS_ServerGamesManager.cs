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
        }
        else
            Destroy(this.gameObject);

    }
    #endregion

    #region Class Methods
    #region Public Methods

    /// <summary>
    /// Method InitializeServerGamesManager, to initialize some data and variables for the server games manager.
    /// </summary>
    public void InitializeServerGamesManager()
    {
        gameServers = new EGS_GameServerData[EGS_Config.MAX_GAMES];
    }

    /// <summary>
    /// Method CheckQueueToStartGame, that check if there are enough players in queue to start a game.
    /// </summary>
    /// <param name="server_socket">Server socket handler</param>
    public void CheckQueueToStartGame(EGS_SE_SocketServer server_socket)
    {
        bool areEnoughForAGame = false;
        List<EGS_PlayerToGame> playersForThisGame = new List<EGS_PlayerToGame>();

        // Lock to evit problems with the queue.
        lock (searchingGame_players)
        {
            // If there are enough players to start a game.
            if (searchingGame_players.Count >= EGS_Config.PLAYERS_PER_GAME)
            {
                areEnoughForAGame = true;
                for (int i = 0; i < EGS_Config.PLAYERS_PER_GAME; i++)
                {
                    // Get the player from the queue.
                    EGS_PlayerToGame playerToGame;
                    searchingGame_players.TryDequeue(out playerToGame);

                    // Add the player to the list of this game.
                    playersForThisGame.Add(playerToGame);
                }
            }
        }

        // If there are enough players for a game:
        if (areEnoughForAGame)
        {
            // Construct the message to send.
            EGS_UpdateData updateData = new EGS_UpdateData();

            for (int i = 0; i < playersForThisGame.Count; i++)
            {
                EGS_PlayerData playerData = new EGS_PlayerData(i, playersForThisGame[i].GetUser().GetUsername());
                playersForThisGame[i].SetIngameID(i);
                updateData.GetPlayersAtGame().Add(playerData);
            }

            // Create the game and get the room number.
            int room = CreateGame(playersForThisGame);

            // Get a list of the users to the game.
            List<EGS_User> usersToGame = new List<EGS_User>();

            foreach (EGS_PlayerToGame playerToGame in playersForThisGame)
            {
                playerToGame.GetUser().SetRoom(room);
                usersToGame.Add(playerToGame.GetUser());
            }

            // Save the users in the room.
            usersInRooms.Add(room, usersToGame);

            updateData.SetRoom(room);

            // Message for the players.
            EGS_Message msg = new EGS_Message();
            msg.messageType = "GAME_FOUND";
            msg.messageContent = JsonUtility.ToJson(updateData);

            string jsonMSG = msg.ConvertMessage();

            // Set the room and message the users so they know that found a game.
            foreach (EGS_User userToGame in usersToGame)
            {
                server_socket.Send(userToGame.GetSocket(), jsonMSG);
            }
        }
    }

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

            // TODO: Put this on a class to serialize as json -> EGS_GameServerStartData.
            // Construct the arguments.
            int gameServerPort = EGS_Config.serverPort + gameServerID + 1;
            string arguments = EGS_Config.serverIP + "#" + EGS_Config.serverPort + "#" + gameServerID + "#" + gameServerPort;
            string jsonString = JsonUtility.ToJson(startData);
            arguments += "#" + jsonString;

            // Save the GameServer data.
            gameServers[gameServerID] = new EGS_GameServerData(gameServerID, room);

            // Try to launch the GameServer.
            try
            {
                gameServers[gameServerID].SetProcess(new Process());
                gameServers[gameServerID].GetProcess().StartInfo.FileName = EGS_Config.GAMESERVER_PATH;
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
