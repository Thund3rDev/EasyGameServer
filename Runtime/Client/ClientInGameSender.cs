using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class ClientInGameSender, which manages the constant message sending to the Game Server.
/// </summary>
public class ClientInGameSender
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


    [Header("Game Loop")]
    [Tooltip("Timer that controls the game loop")]
    private Timer gameLoopTimer;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public ClientInGameSender()
    {
        this.gameRunning = false;
        this.gameLoopTimer = null;
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartGameLoop, that starts the game loop on a thread.
    /// </summary>
    public void StartGameLoop()
    {
        try
        {
            gameLoopTimer = new Timer((e) =>
            {
                Tick();
            }, null, TICK_RATE, TICK_RATE);

            Debug.Log("Started thread for the game.");

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
    public void StopGameLoop()
    {
        // Save that the game is no longer running.
        gameRunning = false;

        if (gameLoopTimer != null)
        {
            try
            {
                gameLoopTimer.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError("Error: " + e);
            }
            Debug.Log("Closed thread for the game.");
        }
    }

    /// <summary>
    /// Method Tick, executed on the game loop FPS times per second.
    /// </summary>
    private void Tick()
    {
        if (!gameRunning)
            return;

        // Call the OnGameSenderTick delegate.
        ClientDelegates.onGameSenderTick?.Invoke();
    }
    #endregion

    #region Getters and Setters
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
    #endregion
}
