using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System;
using System.Linq;

/// <summary>
/// Class ServerGamesManager, that creates and manages the new games.
/// </summary>
public class ServerGamesManager : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static ServerGamesManager instance;


    [Header("Games")]
    [Tooltip("Dictionary that stores users by their room")]
    private Dictionary<int, List<UserData>> usersInRooms = new Dictionary<int, List<UserData>>();

    [Tooltip("ConcurrentQueue that stores users that are searching a game")]
    private ConcurrentQueue<UserData> searchingGameUsers = new ConcurrentQueue<UserData>();

    [Tooltip("Array with the Game Servers")]
    private GameServerData[] gameServersData;


    [Header("Control")]
    [Tooltip("Integer that assigns the room number for the next room")]
    private int nextRoom = 0;

    [Tooltip("Semaphore that controls concurrency for create and delete games")]
    private SemaphoreSlim games_semaphore;
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, called on script load.
    /// </summary>
    private void Awake()
    {
        // Instantiate the singleton.
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
    /// <summary>
    /// Method InitializeServerGamesManager, to initialize some data and variables for the server games manager.
    /// </summary>
    public void InitializeServerGamesManager()
    {
        gameServersData = new GameServerData[EasyGameServerConfig.MAX_GAMES];
        games_semaphore = new SemaphoreSlim(EasyGameServerConfig.MAX_GAMES);

        for (int i = 0; i < EasyGameServerConfig.MAX_GAMES; i++)
            gameServersData[i] = new GameServerData();
    }

    /// <summary>
    /// Method CheckQueueToStartGame, that check if there are enough players in queue to start a game.
    /// </summary>
    /// <param name="server_socket">Server socket handler</param>
    public void CheckQueueToStartGame(MasterServerServerSocketHandler server_socket)
    {
        bool areEnoughForAGame = false;
        List<UserData> usersForThisGame = new List<UserData>();

        // Lock to evit problems with the queue.
        lock (searchingGameUsers)
        {
            // If there are enough players to start a game.
            if (searchingGameUsers.Count >= EasyGameServerConfig.PLAYERS_PER_GAME)
                areEnoughForAGame = true;
        }

        // If enough players to start the game wait until can Create a new Game.
        if (areEnoughForAGame)
        {
            games_semaphore.Wait();
            areEnoughForAGame = false; // Users could have left.

            lock (searchingGameUsers)
            {
                // If there are still enough players to start a game, dequeue the first ones.
                if (searchingGameUsers.Count >= EasyGameServerConfig.PLAYERS_PER_GAME)
                {
                    areEnoughForAGame = true;
                    // Get the users that will play the game.
                    for (int i = 0; i < EasyGameServerConfig.PLAYERS_PER_GAME; i++)
                    {
                        // Dequeue the user.
                        UserData userToGame;
                        searchingGameUsers.TryDequeue(out userToGame);

                        // Add the user to the list of this game.
                        usersForThisGame.Add(userToGame);
                    }
                }
            }
        }

        // If there are enough players for a game:
        if (areEnoughForAGame)
        {
            // Construct the message to send.
            GameFoundData gameFoundData = new GameFoundData();

            for (int i = 0; i < usersForThisGame.Count; i++)
            {
                usersForThisGame[i].SetIngameID(i);
                gameFoundData.GetUsersToGame().Add(usersForThisGame[i]);
            }

            // Create the game and get the updated game found data.
            gameFoundData = CreateGame(gameFoundData);

            // Save the users in the room.
            usersInRooms.Add(gameFoundData.GetRoom(), usersForThisGame);

            // Call the onGameFound delegate.
            MasterServerDelegates.onGameFound?.Invoke(gameFoundData); // Use this delegate to set the scene name.

            // Message for the players.
            string gameFoundMessageContent = JsonUtility.ToJson(gameFoundData);
            NetworkMessage msg = new NetworkMessage(ClientMessageTypes.GAME_FOUND, gameFoundMessageContent);

            string jsonMSG = msg.ConvertMessage();

            // Set the room and message the users so they know that found a game.
            foreach (UserData userToGame in usersForThisGame)
            {
                server_socket.Send(userToGame.GetSocket(), jsonMSG);
            }
        }
    }

    /// <summary>
    /// Method LeaveFromQueue, used when a player wants to leave the Queue.
    /// </summary>
    /// <param name="userLeavingQueue">User leaving the queue</param>
    /// <returns>Bool indicating if was in the queue</returns>
    public bool LeaveFromQueue(UserData userLeavingQueue)
    {
        // Bool to know if user is in queue.
        bool wasUserInQueue = false;

        // Lock the queue.
        lock (searchingGameUsers)
        {
            // Check if user is in queue.
            foreach (UserData userInQueue in searchingGameUsers)
            {
                if (userInQueue.GetUserID() == userLeavingQueue.GetUserID())
                {
                    wasUserInQueue = true;

                    // Remove the player from the Queue by constructing a new queue based on the previous one but without the left user.
                    searchingGameUsers = new ConcurrentQueue<UserData>(searchingGameUsers.Where(x => x.GetUserID() != userLeavingQueue.GetUserID()));

                    break;
                }
            }
        }
        return wasUserInQueue;
    }

    /// <summary>
    /// Method CreateGame, that will create a new instance of a game and assign all data.
    /// </summary>
    /// <param name="gameFoundData">Data of the Game Found</param>
    /// <returns>Updated Game Found Data</returns>
    public GameFoundData CreateGame(GameFoundData gameFoundData)
    {
        // Get the room number.
        int room = Interlocked.Increment(ref nextRoom);
        gameFoundData.SetRoom(room);

        string logString = "<color=#00ffffff>Created game with room </color>" + room + "<color=#00ffffff>. Players: </color>";

        // Assign the room number to the players.
        foreach (UserData userToGame in gameFoundData.GetUsersToGame())
        {
            userToGame.SetRoom(room);
            logString += userToGame.GetUsername() + ", ";
        }

        // Launch the GameServer.
        LaunchGameServer(gameFoundData);

        // Log the information.
        logString = logString.Substring(0, logString.Length - 2) + ".";
        Log.instance.WriteLog(logString, EasyGameServerControl.EnumLogDebugLevel.Minimal);

        // Return the room number.
        return gameFoundData;
    }

    /// <summary>
    /// Method FinishedGame, executed when a game ends.
    /// </summary>
    /// <param name="room">Room number of that game</param>
    /// <param name="gameServerID">ID of the Game Server which managed that game</param>
    public void FinishedGame(int room, int gameServerID)
    {
        // Remove the room number for players.
        foreach (UserData user in usersInRooms[room])
        {
            user.SetRoom(-1);
        }

        usersInRooms.Remove(room);

        // Update the GameServer status.
        UpdateGameServerStatus(gameServerID, GameServerData.EnumGameServerState.INACTIVE);

        // Release one permission of the games semaphore.
        games_semaphore.Release();

        // Display data on the console.
        Log.instance.WriteLog("<color=blue>Disconnected Game Server</color>: ID: " + gameServerID + " - Room: " + room + ".", EasyGameServerControl.EnumLogDebugLevel.Useful);

        // Call the onGameServerClosed delegate.
        MasterServerDelegates.onGameServerClosed?.Invoke(gameServerID);
    }

    /// <summary>
    /// Method QuitUserFromGame, that disconnects an user from a game. 
    /// </summary>
    /// <param name="leftUser">Player's user</param>
    public void QuitUserFromGame(UserData leftUser)
    {
        // Get the game room.
        int room = leftUser.GetRoom();

        // Log into the server.
        Log.instance.WriteLog("<color=#00ffffff>Player</color> " + leftUser.GetUsername() + "<color=#00ffffff> left the game on room </color>" + room + "<color=#00ffffff>.</color>", EasyGameServerControl.EnumLogDebugLevel.Minimal);

        // Update the left user values and remove it from the list of users in rooms.
        leftUser.SetRoom(-1);
        leftUser.SetIngameID(-1);
        usersInRooms[room].Remove(leftUser);
    }

    /// <summary>
    /// Method LaunchGameServer, that launch an instance of the Game Server with the game parameters.
    /// </summary>
    /// <param name="gameFoundData">Game Found data</param>
    private void LaunchGameServer(GameFoundData gameFoundData)
    {
        int gameServerID = -1;
        bool serverAvailable = false;
        int index = 0;

        // Search for an available server.
        while (!serverAvailable && index < EasyGameServerConfig.MAX_GAMES)
        {
            if (gameServersData[index].GetStatus().Equals(GameServerData.EnumGameServerState.INACTIVE))
            {
                serverAvailable = true;
                gameServerID = index;
            }
            else
                index++;
        }

        // Construct the arguments.
        int gameServerPort = EasyGameServerConfig.SERVER_PORT + gameServerID + 1; // This makes the port be one of the next MAX_GAMES ports.
        GameServerArguments arguments = new GameServerArguments(gameServerID, gameServerPort);
        string argumentsJson = JsonUtility.ToJson(arguments);

        // Save the GameServer data.
        gameServersData[gameServerID] = new GameServerData(gameServerID, gameFoundData);

        // Try to launch the GameServer.
        try
        {
            gameServersData[gameServerID].SetProcess(new Process());
            gameServersData[gameServerID].GetProcess().StartInfo.FileName = EasyGameServerConfig.GAMESERVER_PATH;
            gameServersData[gameServerID].GetProcess().StartInfo.Arguments = argumentsJson;
            gameServersData[gameServerID].GetProcess().Start();

            Log.instance.WriteLog("<color=blue>Launched Game Server with parameters: </color>" + argumentsJson, EasyGameServerControl.EnumLogDebugLevel.Extended);
        }
        catch (Exception e)
        {
            Log.instance.WriteErrorLog(e.ToString(), EasyGameServerControl.EnumLogDebugLevel.Minimal);
        }

    }

    /// <summary>
    /// Method ResetRoomNumber, to reset the next room number.
    /// </summary>
    public void ResetRoomNumber()
    {
        nextRoom = 0;
    }

    /// <summary>
    /// Method EnqueueUser, to add an user to the searching game queue.
    /// </summary>
    /// <param name="userToEnqueue">User to enqueue for a game</param>
    public void EnqueueUser(UserData userToEnqueue)
    {
        searchingGameUsers.Enqueue(userToEnqueue);
    }

    /// <summary>
    /// Method GetUsersByRoom, that returns the list of users for an specific room.
    /// </summary>
    /// <param name="room">Room number</param>
    /// <returns>List of users for the specified room</returns>
    public List<UserData> GetUsersByRoom(int room) { return usersInRooms[room]; }

    /// <summary>
    /// Method GetGameServerData, that returns the data of an unique specified Game Server by its ID.
    /// </summary>
    /// <param name="gameServerID">ID of the GameServer</param>
    /// <returns>Game Server Data corresponding to the specified gameServerID</returns>
    public GameServerData GetGameServerData(int gameServerID) { return gameServersData[gameServerID]; }

    /// <summary>
    /// Method UpdateGameServerStatus, that will update the GameServer status to the specified.
    /// </summary>
    /// <param name="gameServerID">ID of the Game Server</param>
    /// <param name="status">Status to change</param>
    public void UpdateGameServerStatus(int gameServerID, GameServerData.EnumGameServerState status)
    {
        gameServersData[gameServerID].SetStatus(status);
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the Dictionary of users in rooms.
    /// </summary>
    /// <returns>Dictionary of users in rooms</returns>
    public Dictionary<int, List<UserData>> GetUsersInRooms() { return usersInRooms; }

    /// <summary>
    /// Setter for the Dictionary of users in rooms.
    /// </summary>
    /// <param name="usersInRooms">New dictionary of users in rooms</param>
    public void SetUsersInRooms(Dictionary<int, List<UserData>> usersInRooms) { this.usersInRooms = usersInRooms; }

    /// <summary>
    /// Getter for the ConcurrentQueue of searching game users.
    /// </summary>
    /// <returns>ConcurrentQueue of searching game users</returns>
    public ConcurrentQueue<UserData> GetSearchingGameUsers() { return searchingGameUsers; }

    /// <summary>
    /// Setter for the ConcurrentQueue of searching game users.
    /// </summary>
    /// <param name="searchingGameUsers">New concurrentQueue of searching game users</param>
    public void SetSearchingGameUsers(ConcurrentQueue<UserData> searchingGameUsers) { this.searchingGameUsers = searchingGameUsers; }

    /// <summary>
    /// Getter for the array of GameServersData.
    /// </summary>
    /// <returns>Array of Game Servers Data</returns>
    public GameServerData[] GetGameServersData() { return gameServersData; }

    /// <summary>
    /// Setter for the array of GameServersData.
    /// </summary>
    /// <param name="gameServers">New array of Game Servers Data</param>
    public void SetGameServersData(GameServerData[] gameServers) { this.gameServersData = gameServers; }
    #endregion
}
