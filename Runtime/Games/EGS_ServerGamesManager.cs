using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System;
using System.Text.RegularExpressions;
using System.Net.Sockets;

/// <summary>
/// Class EGS_ServerGamesManager, that creates and manages the new games.
/// </summary>
public class EGS_ServerGamesManager : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_ServerGamesManager instance;


    [Header("Games")]
    [Tooltip("Dictionary that stores users by their room")]
    public Dictionary<int, List<EGS_User>> usersInRooms = new Dictionary<int, List<EGS_User>>();

    [Tooltip("ConcurrentQueue that stores users that are searching a game")]
    public ConcurrentQueue<EGS_User> searchingGame_Users = new ConcurrentQueue<EGS_User>();

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
        if (instance == null)
        {
            instance = this;
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
    public void CheckQueueToStartGame(EGS_SE_ServerSocket server_socket)
    {
        bool areEnoughForAGame = false;
        List<EGS_User> usersForThisGame = new List<EGS_User>();

        // Lock to evit problems with the queue.
        lock (searchingGame_Users)
        {
            // If there are enough players to start a game.
            if (searchingGame_Users.Count >= EGS_Config.PLAYERS_PER_GAME)
            {
                areEnoughForAGame = true;
                for (int i = 0; i < EGS_Config.PLAYERS_PER_GAME; i++)
                {
                    // Get the player from the queue.
                    EGS_User userToGame;
                    searchingGame_Users.TryDequeue(out userToGame);

                    // Add the user to the list of this game.
                    usersForThisGame.Add(userToGame);
                }
            }
        }

        // If there are enough players for a game:
        if (areEnoughForAGame)
        {
            // Construct the message to send.
            EGS_GameFoundData gameFoundData = new EGS_GameFoundData();

            for (int i = 0; i < usersForThisGame.Count; i++)
            {
                usersForThisGame[i].SetIngameID(i);
                gameFoundData.GetUsersToGame().Add(usersForThisGame[i]);
            }

            // Create the game and get the room number.
            int room = CreateGame(gameFoundData);

            // Set the room for the users.
            foreach (EGS_User userToGame in usersForThisGame)
            {
                userToGame.SetRoom(room);
            }

            // Save the users in the room.
            usersInRooms.Add(room, usersForThisGame);

            // Update the game found data room.
            //gameFoundData.SetRoom(room);

            // Message for the players.
            EGS_Message msg = new EGS_Message();
            msg.messageType = "GAME_FOUND";
            msg.messageContent = JsonUtility.ToJson(gameFoundData);

            string jsonMSG = msg.ConvertMessage();

            // Set the room and message the users so they know that found a game.
            foreach (EGS_User userToGame in usersForThisGame)
            {
                server_socket.Send(userToGame.GetSocket(), jsonMSG);
            }

            // Call the onGameFound delegate.
            EGS_MasterServerDelegates.onGameFound?.Invoke(gameFoundData);
        }
    }

    /// <summary>
    /// Method CreateGame, that will create a new instance of a game and assign all data.
    /// </summary>
    /// <param name="usersToGame">List of users that will play that game</param>
    /// <returns>Room number</returns>
    public int CreateGame(EGS_GameFoundData gameFoundData)
    {
        // Get the room number.
        int room = Interlocked.Increment(ref nextRoom);
        gameFoundData.SetRoom(room);

        string logString = "Created game with room " + room + ". Players: ";

        // Assign the room number to the players.
        foreach (EGS_User userToGame in gameFoundData.GetUsersToGame())
        {
            userToGame.SetRoom(room);
            logString += userToGame.GetUsername() + ", ";
        }

        // Launch the GameServer.
        LaunchGameServer(gameFoundData);

        // Log the information.
        logString = logString.Substring(0, logString.Length - 2) + ".";
        egs_Log.Log(logString);

        // Return the room number.
        return room;
    }

    /// <summary>
    /// Method FinishedGame, executed when a game ends.
    /// </summary>
    /// <param name="roomID">Room number of that game</param>
    /// <param name="gameServerID">ID of the Game Server which managed that game</param>
    /// <param name="handler">Socket of the Game Server</param>
    public void FinishedGame(int roomID, int gameServerID, Socket handler)
    {
        // Remove the room number for players.
        foreach (EGS_User user in usersInRooms[roomID])
        {
            user.SetRoom(-1);
        }

        usersInRooms.Remove(roomID);

        // Update the GameServer status.
        gameServers[gameServerID].SetStatus(EGS_GameServerData.EGS_GameServerState.INACTIVE);

        // Display data on the console.
        if (EGS_Config.DEBUG_MODE > -1)
            egs_Log.Log("<color=blue>Disconnected Game Server</color>: ID: " + gameServerID + " - Room: " + roomID + ".");

        // Call the onGameServerClosed delegate.
        EGS_MasterServerDelegates.onGameServerClosed?.Invoke(gameServerID);
    }

    /// <summary>
    /// Method QuitUserFromGame, that disconnects an user from a game. 
    /// </summary>
    /// <param name="leftUser">Player's user</param>
    public void QuitUserFromGame(EGS_User leftUser)
    {
        // TODO: Make this work. Think on a EGS_PlayerToGame for the parameter.
        int room = leftUser.GetRoom();

        egs_Log.Log("Player " + leftUser.GetUsername() + " left the game on room " + room + ".");

        leftUser.SetRoom(-1);
        leftUser.SetIngameID(-1);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method LaunchGameServer, that launch an instance of the Game Server with the game parameters.
    /// </summary>
    /// <param name="room">Room number</param>
    /// <param name="playersToGame">List of users to play that game</param>
    private void LaunchGameServer(EGS_GameFoundData gameFoundData)
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
            // TODO: Put this on a class to serialize as json -> EGS_GameServerStartData.
            // Construct the arguments.
            int gameServerPort = EGS_Config.serverPort + gameServerID + 1;
            string arguments = EGS_Config.serverIP + "#" + EGS_Config.serverPort + "#" + gameServerID + "#" + gameServerPort;

            // Save the GameServer data.
            gameServers[gameServerID] = new EGS_GameServerData(gameServerID, gameFoundData);

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
