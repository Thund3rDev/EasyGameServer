using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class RoundTripTime, that controls the connection of a client and indicate its ping.
/// </summary>
public class RoundTripTime
{
    #region Variables
    [Header("Control")]
    [Tooltip("Timer that controls if the client didn't return packages in a while")]
    private System.Timers.Timer timeoutTimer;

    [Tooltip("Thread that controls the Round Trip Time")]
    private Thread roundTripTimeThread;

    [Tooltip("Stopwatch to calculate the time for the RTT")]
    private Stopwatch roundTripTimeStopwatch;

    [Tooltip("Bool that indicates if it is stilLConnected")]
    private bool stillConnected;

    [Tooltip("Mutex to don't let change the value of stillConnected in the middle of the execution")]
    private Mutex connectedMutex = new Mutex();

    [Tooltip("Type of the client for the RTT")]
    private EasyGameServerControl.EnumInstanceType clientType;


    [Header("References")]
    [Tooltip("Reference to the server socket controller")]
    private ServerSocketHandler socketController;

    [Tooltip("Reference to the client socket assigned")]
    private Socket client_socket;


    [Header("Useful")]
    [Tooltip("Time in milliseconds for the RTT to be sent and received")]
    private long lastRTTMilliseconds = 0;
    #endregion

    #region Constructors
    /// <summary>
    /// Base Constructor.
    /// </summary>
    /// <param name="socketController">Reference to the server socket controller</param>
    /// <param name="client_socket">Reference to the client socket assigned</param>
    /// <param name="clientType">Type of client</param>
    public RoundTripTime(ServerSocketHandler socketController, Socket client_socket, EasyGameServerControl.EnumInstanceType clientType)
    {
        // Assign references.
        this.socketController = socketController;
        this.client_socket = client_socket;
        this.clientType = clientType;

        // Initialize variables.
        this.roundTripTimeStopwatch = new Stopwatch();
        this.roundTripTimeThread = new Thread(() => RTT());
        this.stillConnected = true;

        // Start the timeout timer and the RTT.
        StartTimeoutTimer();
        StartRTT();
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method ReceiveRTT, that resets the timeoutTimer and calculates the time the RTT lasted.
    /// </summary>
    /// <returns>RTT time in milliseconds</returns>
    public long ReceiveRTT()
    {
        timeoutTimer.Stop();
        timeoutTimer.Start();
        return CalculateTime();
    }

    /// <summary>
    /// Method StopRTT, that stops the Round Trip Time closing the timer and cancelling the RTT Thread.
    /// </summary>
    public void StopRTT()
    {        
        timeoutTimer.Stop();
        timeoutTimer.Close();

        connectedMutex.WaitOne();
        stillConnected = false;
        connectedMutex.ReleaseMutex();
    }

    /// <summary>
    /// Method StartTimeoutTimer, that sets and starts a timer for the disconnection.
    /// </summary>
    private void StartTimeoutTimer() {
        timeoutTimer = new System.Timers.Timer(EasyGameServerConfig.DISCONNECT_TIMEOUT);
        timeoutTimer.Start();
        timeoutTimer.Elapsed += (sender, e) => socketController.DisconnectClientByTimeout(sender, e, client_socket, clientType);
    }

    /// <summary>
    /// Method StartRTT, that starts the Round Trip Time Thread.
    /// </summary>
    private void StartRTT()
    {
        roundTripTimeThread.Start();
    }

    /// <summary>
    /// Method RTT, that sends a Round Trip Time while the client is still connected.
    /// </summary>
    private void RTT()
    {
        // While client is still connected to the server.
        while (stillConnected)
        {
            connectedMutex.WaitOne();

            // Checking that still connected to be sure it didn't change before the mutex.
            if (!stillConnected)
                return;

            // Create the message to be send.
            NetworkMessage msg = new NetworkMessage(ClientMessageTypes.RTT, lastRTTMilliseconds.ToString());
            string jsonMSG = msg.ConvertMessage();

            // Try to send the message to the client.
            try
            {
                if (client_socket.Connected)
                    socketController.Send(client_socket, jsonMSG);
            }
            catch (Exception)
            {
                // LOG.
            }
            finally
            {
                connectedMutex.ReleaseMutex();
            }

            // Start the time for another RTT.
            if (!roundTripTimeStopwatch.IsRunning)
                roundTripTimeStopwatch.Start();

            // Sleep to wait until next RTT.
            Thread.Sleep(EasyGameServerConfig.TIME_BETWEEN_RTTS);
        }
    }

    /// <summary>
    /// Method CalculateTime, that stops and resets the stopwatch and calculates the time the RTT lasted.
    /// </summary>
    /// <returns>RTT time in milliseconds</returns>
    private long CalculateTime()
    {
        roundTripTimeStopwatch.Stop();
        lastRTTMilliseconds = roundTripTimeStopwatch.ElapsedMilliseconds;
        roundTripTimeStopwatch.Reset();

        return lastRTTMilliseconds;
    }
    #endregion

    #region Getters And Setters
    /// <summary>
    /// Method GetLastRTT, to get the last RTT in milliseconds.
    /// </summary>
    /// <returns>Long with the last RTT in milliseconds</returns>
    public long GetLastRTT() { return lastRTTMilliseconds; }
    #endregion
}
