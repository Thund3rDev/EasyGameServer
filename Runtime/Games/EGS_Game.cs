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
    public static float playerAPosX = -0.05f;
    public static float playerAPosY = -0.05f;
    public static float playerBPosX = 0.05f;
    public static float playerBPosY = 0.05f;

    // Operation variables.
    private EGS_Message messageToSend = new EGS_Message();
    private Timer timerLoop;
    private Mutex threadLock = new Mutex();

    // Control variables.
    private bool gameStarted;
    private int room;

    // EGS.
    private EGS_Log egs_Log;
    private EGS_SE_SocketController socketController;

    // Game.
    private List<EGS_Player> players = new List<EGS_Player>();
    private List<EGS_Player> playersCopy = new List<EGS_Player>();

    #endregion


    public EGS_Game(EGS_Log e, EGS_SE_SocketController sc, List<EGS_Player> players_, int room_)
    {
        egs_Log = e;
        socketController = sc;
        players = players_;
        room = room_;
    }

    public bool IsGameStarted()
    {
        return gameStarted;
    }
    public void SetGameStarted(bool gameStarted)
    {
        this.gameStarted = gameStarted;
    }

    public List<EGS_Player> GetPlayers()
    {
        return players;
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
            egs_Log.LogError("Error: " + e);
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
                egs_Log.LogError("Error: " + e);
            }
            finally
            {
                threadLock.ReleaseMutex();
            }
            egs_Log.Log("Closed thread for the game on room " + room);
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
                egs_Log.Log("Disconnected player " + p.GetUser().GetUsername() + " from game at room: " + room);
            }
            catch (Exception e)
            {
                egs_Log.LogError("Error: " + e);
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
            egs_Log.LogError("Error: " + e);
        }
    }
}
