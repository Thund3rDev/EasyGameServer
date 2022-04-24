using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System;

/// <summary>
/// Class EGS_Game, that controls the game loop and save the game data.
/// </summary>
public class EGS_Game
{
    #region Variables
    [Header("Fixed Variables")]
    [Tooltip("Frames per second, number of server calculations in a second")]
    private readonly static int FPS = EGS_Config.CALCULATIONS_PER_SECOND;
    [Tooltip("Tick Rate, time between server calculations")]
    private readonly static long TICK_RATE = 1000 / FPS; // 1000 ms -> 1 second.


    [Header("Control Variables")]
    [Tooltip("Bool that indicates if the game is running")]
    private bool gameRunning;

    [Tooltip("Room number")]
    private int room;

    [Tooltip("Game Scene name")]
    private string gameSceneName;


    [Header("Game Start Control")]
    [Tooltip("Lock for protect concurrent user confirmations to start the game")]
    private Mutex startGame_Lock;

    [Tooltip("Integer that counts the number of players confirmed to start the game")]
    private int startGame_Counter;


    [Header("Game Loop")]
    [Tooltip("Timer that controls the game loop")]
    private Timer gameLoopTimer;

    [Tooltip("Lock to control the game loop thread")]
    private Mutex gameLoopLock = new Mutex();


    [Header("References")]
    [Tooltip("Reference to the server socket controller")]
    private EGS_GS_ServerSocket socketController;


    [Header("Game data")]
    [Tooltip("List of players")]
    private List<EGS_Player> players = new List<EGS_Player>();

    [Tooltip("Copy of the list of players to don't touch the original")]
    private List<EGS_Player> playersCopy = new List<EGS_Player>();
    #endregion

    #region Constructors
    /// <summary>
    /// Base Constructor
    /// </summary>
    /// <param name="sc">Reference of the server socket controller</param>
    /// <param name="room_">Room number</param>
    /// <param name="gameSceneName_">Name of the Game Scene</param>
    public EGS_Game(EGS_GS_ServerSocket sc, int room_, string gameSceneName_)
    {
        socketController = sc;
        room = room_;
        gameSceneName = gameSceneName_;
        startGame_Lock = new Mutex();
    }
    #endregion

    #region Class Methods
    #region Public Methods
    /// <summary>
    /// Method AddPlayer, that adds a player to the game.
    /// </summary>
    /// <param name="playerToAdd">Player to add to the game</param>
    public void AddPlayer(EGS_Player playerToAdd)
    {
        players.Add(playerToAdd);
    }

    /// <summary>
    /// Method Ready, that checks in a player for the game and returns if all players are to start.
    /// </summary>
    /// <returns>Bool that indicates if game can start</returns>
    public bool Ready()
    {
        // Define a bool to control if game can start.
        bool canGameStart = false;

        // Lock the access.
        startGame_Lock.WaitOne();

        // Check if all players are ready to start the game.
        int playersReady = ++startGame_Counter;

        if (playersReady == EGS_Config.PLAYERS_PER_GAME)
        {
            // Start the game loop and tell that game can start.
            StartGameLoop();
            canGameStart = true;
        }

        // Unlock the access.
        startGame_Lock.ReleaseMutex();

        // Return if game can start.
        return canGameStart;
    }

    /// <summary>
    /// Method FinishGame, that ends the game loop and sends the game results to the server.
    /// </summary>
    public void FinishGame(List<int> playerIDsOrdered)
    {
        // If game is not running, do nothing.
        if (!gameRunning)
            return;

        // Stop the game loop for that game.
        StopGameLoop();

        // Create a message indicating that game has finished.
        EGS_Message messageToSend = new EGS_Message();
        messageToSend.messageType = "GAME_END";

        EGS_GameEndData gameEndData = new EGS_GameEndData(EGS_GameServer.instance.gameServerID, room, playerIDsOrdered);
        messageToSend.messageContent = JsonUtility.ToJson(gameEndData);

        // Call the OnEndGame delegate.
        EGS_GameServerDelegates.onGameEnd?.Invoke(gameEndData);

        // Show the EndGame Info.
        EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServerEndController.instance.ShowEndGameInfo(); });

        // Send the message to the players.
        Broadcast(messageToSend);

        // Also send the message to the Master Server.
        EGS_GameServer.instance.gameServerSocketsController.SendMessageToMasterServer(messageToSend);
    }

    /// <summary>
    /// Method QuitPlayerFromGame, that disconnects a player from the game server and tells it to connect to the master server.
    /// </summary>
    /// <param name="leftPlayer">Player who disconnected from the game</param>
    public void QuitPlayerFromGame(EGS_Player leftPlayer)
    {
        lock (players)
        {
            players.Remove(leftPlayer);

            // TODO: Tell the master server that the player left the game.

            if (players.Count == 0)
                FinishGame(new List<int>());
        }
    }

    /// <summary>
    /// Method StartGameLoop, that starts the game loop on a thread.
    /// </summary>
    private void StartGameLoop()
    {
        try
        {
            gameLoopTimer = new Timer((e) =>
            {
                Tick();
            }, null, TICK_RATE, TICK_RATE);

            // Save that the game is currently running.
            gameRunning = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e);
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
                gameLoopLock.WaitOne();
                gameLoopTimer.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError("Error: " + e);
            }
            finally
            {
                gameLoopLock.ReleaseMutex();
            }
            Debug.Log("Closed thread for the game on room " + room);
        }
    }

    /// <summary>
    /// Method Broadcast, that sends a message to all the players in the game.
    /// </summary>
    /// <param name="message">Message to be sent</param>
    public void Broadcast(EGS_Message message)
    {
        // Make a copy of the player to avoid errors on sends.
        lock (players)
        {
            playersCopy = new List<EGS_Player>(players);
        }

        foreach (EGS_Player p in playersCopy)
        {
            try
            {
                // Try to send the message to the player.
                gameLoopLock.WaitOne();
                socketController.Send(p.GetUser().GetSocket(), message.ConvertMessage());
            }
            catch (SocketException)
            {
                // If fails with a SocketException, player closed the socket on his side, disconnect and remove it.
                socketController.DisconnectClient(p.GetUser().GetSocket());
                players.Remove(p);
                Debug.Log("Disconnected player " + p.GetUser().GetUsername() + " from game at room: " + room);
            }
            catch (Exception e)
            {
                Debug.LogError("Error: " + e);
            }
            finally
            {
                gameLoopLock.ReleaseMutex();
            }
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method Tick, executed on the game loop FPS times per second. 
    /// </summary>
    private void Tick()
    {
        if (!gameRunning)
            return;

        try
        {
            // Create the message to be sent to the players.
            EGS_Message msg = new EGS_Message();
            msg.messageType = "UPDATE";

            EGS_UpdateData updateData = new EGS_UpdateData(room);

            // Call the OnTick delegate.
            EGS_GameServerDelegates.onTick?.Invoke(updateData);

            // For each player, calculate its position and save its data to the Update Data.
            foreach (EGS_Player player in players)
            {
                // Call the OnProcessPlayer delegate.
                EGS_GameServerDelegates.onProcessPlayer?.Invoke(player, updateData, TICK_RATE);
            }

            // Save the message content and send it to the players.
            msg.messageContent = JsonUtility.ToJson(updateData);

            Broadcast(msg);
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e);
        }
    }
    #endregion
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
    public void SetGameRunning(bool gameRunning_) { this.gameRunning = gameRunning_; }

    /// <summary>
    /// Getter for the room number.
    /// </summary>
    /// <returns>Room number</returns>
    public int GetRoom() { return room; }

    /// <summary>
    /// Setter for the room number.
    /// </summary>
    /// <param name="room_">New room number</param>
    public void SetRoom(int room_) { room = room_; }

    /// <summary>
    /// Getter for the list of players.
    /// </summary>
    /// <returns>List of players in the game</returns>
    public List<EGS_Player> GetPlayers() { return players; }


    /// <summary>
    /// Getter for the Game Scene Name.
    /// </summary>
    public string GetGameSceneName() { return gameSceneName; }

    /// <summary>
    /// Setter for the Game Scene Name.
    /// </summary>
    /// <param name="gameSceneName_">New Game Scene Name</param>
    public void SetGameSceneName(string gameSceneName_) { gameSceneName = gameSceneName_; }

    
    #endregion
}
