using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System;

/// <summary>
/// Class Game, that controls the game loop and save the game data.
/// </summary>
public class Game
{
    #region Variables
    [Header("Fixed Variables")]
    [Tooltip("Frames per second, number of server calculations in a second")]
    private readonly static int FPS = EasyGameServerConfig.CALCULATIONS_PER_SECOND;
    [Tooltip("Tick Rate, time between server calculations")]
    private readonly static long TICK_RATE = 1000 / FPS; // 1000 ms -> 1 second.


    [Header("Control Variables")]
    [Tooltip("Bool that indicates if the game is running")]
    private bool gameRunning;

    [Tooltip("Room number")]
    private int room;

    [Tooltip("Name of the scene where the game will run")]
    private string gameSceneName;


    [Header("Game Start Control")]
    [Tooltip("Lock for protect concurrent user confirmations to start the game")]
    private Mutex startGame_mutex;

    [Tooltip("Integer that counts the number of players confirmed to start the game")]
    private int startGameCounter;


    [Header("Game Loop")]
    [Tooltip("Timer that controls the game loop")]
    private Timer gameLoopTimer;

    [Tooltip("Lock to control the game loop thread")]
    private Mutex gameLoop_mutex = new Mutex();


    [Header("References")]
    [Tooltip("Reference to the server socket controller")]
    private GameServerServerSocketHandler socketController;


    [Header("Game data")]
    [Tooltip("List of players")]
    private List<NetworkPlayer> players = new List<NetworkPlayer>();

    [Tooltip("Copy of the list of players to don't touch the original")]
    private List<NetworkPlayer> playersCopy = new List<NetworkPlayer>();

    [Tooltip("Stack that registers the order of players when they leave so they are the last in the game")]
    private Stack<int> playerIDsOrderStack;
    #endregion

    #region Constructors
    /// <summary>
    /// Base Constructor.
    /// </summary>
    /// <param name="sc">Reference of the server socket controller</param>
    /// <param name="room_">Room number</param>
    /// <param name="gameSceneName_">Name of the Game Scene</param>
    public Game(GameServerServerSocketHandler sc, int room_, string gameSceneName_)
    {
        this.socketController = sc;
        this.room = room_;
        this.gameSceneName = gameSceneName_;
        this.startGame_mutex = new Mutex();
        this.playerIDsOrderStack = new Stack<int>();
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method AddPlayer, that adds a player to the game.
    /// </summary>
    /// <param name="playerToAdd">Player to add to the game</param>
    public void AddPlayer(NetworkPlayer playerToAdd)
    {
        this.players.Add(playerToAdd);
    }

    /// <summary>
    /// Method Ready, that checks in a player for the game and returns if all players are to start.
    /// </summary>
    /// <returns>Bool that indicates if game can start</returns>
    public bool Ready()
    {
        // Define a bool to control if game can start.
        bool canGameStart = false;

        try
        {
            // Lock the access.
            this.startGame_mutex.WaitOne();

            // Check if all players are ready to start the game.
            int playersReady = ++startGameCounter;

            if (playersReady == EasyGameServerConfig.PLAYERS_PER_GAME)
            {
                // Start the game loop and tell that game can start.
                StartGameLoop();
                canGameStart = true;
            }
        }
        catch (Exception)
        {
            // LOG.
        }
        finally
        {
            // Unlock the access.
            startGame_mutex.ReleaseMutex();
        }

        // Return if game can start.
        return canGameStart;
    }

    /// <summary>
    /// Method FinishGame, that ends the game loop and sends the game results to the server.
    /// </summary>
    public void FinishGame(List<int> playerIDsOrdered, bool endedAsDisconnection)
    {
        // If game is not running, do nothing.
        if (!gameRunning)
            return;

        // Stop the game loop for that game.
        StopGameLoop();

        // Create and get the Game end data.
        GameEndData gameEndData = new GameEndData(GameServer.instance.GetGameServerID(), room, playerIDsOrdered, endedAsDisconnection);
        string gameEndMessageContent = JsonUtility.ToJson(gameEndData);

        // Call the OnEndGame delegate.
        GameServerDelegates.onGameEnd?.Invoke(gameEndData);

        // Show the EndGame Info.
        MainThreadDispatcher.RunOnMainThread(() => { GameServerEndController.instance.ShowEndGameInfo(); });

        // Create a message indicating that game has finished.
        NetworkMessage messageToSend = new NetworkMessage("GAME_END", gameEndMessageContent);

        // Send the message to the players.
        Broadcast(messageToSend);

        // Also send the message to the Master Server.
        GameServer.instance.SendMessageToMasterServer(messageToSend);
    }

    /// <summary>
    /// Method QuitPlayerFromGame, that disconnects a player from the game server and tells it to connect to the master server.
    /// </summary>
    /// <param name="leftPlayer">Player who disconnected from the game</param>
    public void QuitPlayerFromGame(NetworkPlayer leftPlayer)
    {
        lock (players)
        {
            players.Remove(leftPlayer);
            leftPlayer.GetUser().SetLeftGame(true);
            playerIDsOrderStack.Push(leftPlayer.GetIngameID());

            // Remove the player from the GameManager.
            NetworkGameManager.instance.GetPlayersInGame().Remove(leftPlayer);

            // Tell the master server that the player left the game.
            string userJson = JsonUtility.ToJson(leftPlayer.GetUser());
            GameServer.instance.SendMessageToMasterServer("USER_LEAVE_GAME", userJson);

            // Tell the players that a player left the game.
            NetworkMessage playerLeftMessage = new NetworkMessage("PLAYER_LEAVE_GAME", leftPlayer.GetIngameID().ToString());
            Broadcast(playerLeftMessage);

            // Check if only one player remains.
            if (players.Count == 1)
            {
                List<int> playerIDsOrdered = new List<int>();
                playerIDsOrdered.Add(players[0].GetIngameID()); // Winner.

                for (int i = 0; i < playerIDsOrderStack.Count; i++)
                {
                    playerIDsOrdered.Add(playerIDsOrderStack.Pop());
                }

                FinishGame(playerIDsOrdered, true);
            }
        }
    }

    /// <summary>
    /// Method StartGameLoop, that starts the game loop on a thread.
    /// </summary>
    private void StartGameLoop()
    {
        try
        {
            // Try to start the timer.
            gameLoop_mutex.WaitOne();
            gameLoopTimer = new Timer((e) =>
            {
                Tick();
            }, null, TICK_RATE, TICK_RATE);

            // Save that the game is currently running.
            gameRunning = true;
        }
        catch (Exception e)
        {
            // LOG.
            Debug.LogError("Error: " + e);
        }
        finally
        {
            // Release the Mutex.
            gameLoop_mutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// Method StopGameLoop, that stops the game loop.
    /// </summary>
    private void StopGameLoop()
    {
        // Save that the game is no longer running.
        gameRunning = false;

        if (gameLoopTimer != null)
        {
            try
            {
                // Try to stop the timer.
                gameLoop_mutex.WaitOne();
                gameLoopTimer.Dispose();
            }
            catch (Exception e)
            {
                // LOG.
                Debug.LogError("Error: " + e);
            }
            finally
            {
                // Release the Mutex.
                gameLoop_mutex.ReleaseMutex();
            }
            // LOG.
            Debug.Log("Closed thread for the game on room " + room);
        }
    }

    /// <summary>
    /// Method Broadcast, that sends a message to all the players in the game.
    /// </summary>
    /// <param name="message">Message to be sent</param>
    public void Broadcast(NetworkMessage message)
    {
        // Make a copy of the player to avoid errors on sends.
        lock (players)
        {
            playersCopy = new List<NetworkPlayer>(players);
        }

        foreach (NetworkPlayer player in playersCopy)
        {
            try
            {
                // Try to send the message to the player.
                gameLoop_mutex.WaitOne();
                socketController.Send(player.GetUser().GetSocket(), message.ConvertMessage());
            }
            catch (SocketException)
            {
                // If fails with a SocketException, player closed the socket on his side, disconnect and remove it.
                // Get the user.
                UserData userToDisconnect = player.GetUser();

                // Remove the player from the game.
                QuitPlayerFromGame(player);

                // DisconnectFromMasterServer the user from the server.
                socketController.DisconnectUserBySocketException(userToDisconnect);

                // Call the onPlayerLeaveGame delegate.
                GameServerDelegates.onPlayerLeaveGame?.Invoke(player);

                // Log.
                /*if (EasyGameServerConfig.DEBUG_MODE_CONSOLE > -1)
                    Log.instance.Log("<color=blue>Disconnected by timeout [CLIENT]</color>: UserID: " + userToDisconnect.GetUserID() + " - Username: " + userToDisconnect.GetUsername() + " - IP: " + userToDisconnect.GetIPAddress() + ".");*/
            }
            catch (Exception e)
            {
                // Log.
                Debug.LogError("Error: " + e);
            }
            finally
            {
                // Release the Mutex.
                gameLoop_mutex.ReleaseMutex();
            }
        }
    }

    /// <summary>
    /// Method Tick, executed on the game loop FPS times per second. 
    /// </summary>
    private void Tick()
    {
        if (!gameRunning)
            return;

        try
        {
            // Create a new Update Data.
            UpdateData updateData = new UpdateData(room);

            // Call the OnTick delegate.
            GameServerDelegates.onTick?.Invoke(updateData);

            // For each player, calculate its position and save its data to the Update Data.
            foreach (NetworkPlayer player in players)
            {
                // Call the OnProcessPlayer delegate.
                GameServerDelegates.onProcessPlayer?.Invoke(player, updateData, TICK_RATE);
            }

            // Create the message content.
            string updateDataMessageContent = JsonUtility.ToJson(updateData);

            // Create the message and sent it to the players.
            NetworkMessage msg = new NetworkMessage("UPDATE", updateDataMessageContent);
            Broadcast(msg);
        }
        catch (Exception e)
        {
            // Log.
            Debug.LogError("Error: " + e);
        }
    }
    #endregion

    #region Getter and Setters
    /// <summary>
    /// Getter for the GameRunning bool.
    /// </summary>
    /// <returns>Game Running value</returns>
    public bool IsGameRunning() { return gameRunning; }

    /// <summary>
    /// Setter for the GameRunning bool.
    /// </summary>
    /// <param name="gameRunning">New Game Running value</param>
    public void SetGameRunning(bool gameRunning) { this.gameRunning = gameRunning; }

    /// <summary>
    /// Getter for the room number.
    /// </summary>
    /// <returns>Room number</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the room number.
    /// </summary>
    /// <param name="room">New room number</param>
    public void SetRoom(int room) { this.room = room; }

    /// <summary>
    /// Getter for the list of players.
    /// </summary>
    /// <returns>List of players in the game</returns>
    public List<NetworkPlayer> GetPlayers() { return players; }


    /// <summary>
    /// Getter for the Game Scene Name.
    /// </summary>
    public string GetGameSceneName() { return gameSceneName; }

    /// <summary>
    /// Setter for the Game Scene Name.
    /// </summary>
    /// <param name="gameSceneName">New Game Scene Name</param>
    public void SetGameSceneName(string gameSceneName) { this.gameSceneName = gameSceneName; }
    #endregion
}
