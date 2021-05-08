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
    private EGS_SE_SocketListener socketListener;

    // Game.
    Dictionary<string, EGS_Player> players = new Dictionary<string, EGS_Player>();

    #endregion


    public EGS_Game(EGS_Log e, EGS_SE_SocketListener sl, Dictionary<string, EGS_Player> players_, int room_)
    {
        egs_Log = e;
        socketListener = sl;
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

    public List<EGS_Player> getPlayers()
    {
        List<EGS_Player> playersToGet = new List<EGS_Player>();
        foreach (EGS_Player p in players.Values)
        {
            playersToGet.Add(p);
        }

        return playersToGet;
    }

    public void StartGameLoop()
    {
        try
        {
            egs_Log.Log("StartGameLoop");
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
        egs_Log.Log("StopGameLoop");
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
        foreach (EGS_Player p in players.Values)
        {
            try
            {
                threadLock.WaitOne();
                socketListener.Send(p.GetUser().getSocket(), message.ConvertMessage());
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
            /*msg.messageType = "UPDATE";
            msg.messageContent = "";

            foreach (EGS_Player p in players.Values)
            {
                p.CalculatePosition();

                Vector3 playerPos = p.GetPosition();
                msg.messageContent += p.GetUser().getUsername() + "-" + playerPos.x + "|" + playerPos.y + "|" + playerPos.z;
                msg.messageContent += ";";
            }*/
            //egs_log.Log(msg.ConvertMessage());

            foreach (EGS_Player p in players.Values)
            {
                p.CalculatePosition(TICK_RATE);
                Vector3 playerPos = p.GetPosition();
                msg.messageType = "POSITION";
                msg.messageContent += playerPos.x + "|" + playerPos.y + "|" + playerPos.z;
            }

            Broadcast(msg);
        }
        catch (Exception e)
        {
            egs_Log.LogError("Error: " + e);
        }
    }
}
