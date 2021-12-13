using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System;

public class EGS_Game
{
    #region Variables
    // Generic variables.
    private readonly static int FPS = 60;
    private readonly static long TICK_RATE = 1000 / FPS;
    public readonly static bool DEBUG_MODE = true;

    // Positions.
    /*public static float playerAPosX = -0.05f;
    public static float playerAPosY = -0.05f;
    public static float playerBPosX = 0.05f;
    public static float playerBPosY = 0.05f;*/

    // Operation variables.
    private EGS_Message messageToSend = new EGS_Message();
    private Timer timerLoop;
    private Mutex threadLock = new Mutex();

    // Control variables.
    private bool gameStarted;
    private int room;
    private Mutex startGame_Lock;
    private int startGame_Counter;

    // EGS.
    private EGS_GS_SocketServer socketController;

    // Game.
    private List<EGS_Player> players = new List<EGS_Player>();
    private List<EGS_Player> playersCopy = new List<EGS_Player>();

    #endregion

    public EGS_Game(EGS_GS_SocketServer sc, int room_)
    {
        socketController = sc;
        room = room_;
        startGame_Lock = new Mutex();
    }

    public bool IsGameStarted()
    {
        return gameStarted;
    }
    public void SetGameStarted(bool gameStarted)
    {
        this.gameStarted = gameStarted;
    }

    public int GetRoom()
    {
        return room;
    }

    public void SetRoom(int room_)
    {
        room = room_;
    }

    public List<EGS_Player> GetPlayers()
    {
        return players;
    }

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

        if (playersReady == EGS_GameServer.gameServer_instance.PLAYERS_PER_GAME)
        {
            StartGameLoop();
            canGameStart = true;
        }

        // Unlock the access.
        startGame_Lock.ReleaseMutex();

        // Return if game can start.
        return canGameStart;
    }

    public void FinishGame()
    {
        // Stop the game loop for that game.
        StopGameLoop();

        // TODO: Tell the master server that the game finished and what were the results.
    }

    public void QuitPlayerFromGame(EGS_Player leftPlayer)
    {
        int room = leftPlayer.GetRoom();

        lock (players)
        {
            players.Remove(leftPlayer);

            // TODO: Tell the master server that the player left the game.

            if (players.Count == 0)
                FinishGame();
        }
    }

    public void StartGameLoop()
    {
        try
        {
            timerLoop = new System.Threading.Timer((e) =>
            {
                Tick();
            }, null, TICK_RATE, TICK_RATE);

            gameStarted = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e);
        }
    }

    public void StopGameLoop()
    {
        gameStarted = false;

        if (timerLoop != null)
        {
            try
            {
                threadLock.WaitOne();
                timerLoop.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError("Error: " + e);
            }
            finally
            {
                threadLock.ReleaseMutex();
            }
            Debug.Log("Closed thread for the game on room " + room);
        }
    }

    public void Broadcast(EGS_Message message)
    {
        lock (players)
        {
            playersCopy = new List<EGS_Player>(players);
        }

        foreach (EGS_Player p in playersCopy)
        {
            try
            {
                threadLock.WaitOne();
                socketController.Send(p.GetUser().GetSocket(), message.ConvertMessage());
            }
            catch (SocketException)
            {
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
                threadLock.ReleaseMutex();
            }
        }
    }

    private void Tick()
    {
        if (!gameStarted)
            return;

        try
        {
            EGS_Message msg = new EGS_Message();
            msg.messageType = "UPDATE";

            EGS_UpdateData updateData = new EGS_UpdateData(room);

            foreach (EGS_Player player in players)
            {
                player.CalculatePosition(TICK_RATE);
                EGS_PlayerData playerData = new EGS_PlayerData(player.GetIngameID(), player.GetUser().GetUsername(), player.GetPosition());
                updateData.GetPlayersAtGame().Add(playerData);
            }

            msg.messageContent = JsonUtility.ToJson(updateData);

            Broadcast(msg);
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e);
        }
    }
}
