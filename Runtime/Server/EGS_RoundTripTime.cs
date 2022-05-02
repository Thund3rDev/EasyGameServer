using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_RoundTripTime, that controls the connection of a client and indicate its ping.
/// </summary>
public class EGS_RoundTripTime
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
    private EGS_Control.EGS_Type clientType;


    [Header("References")]
    [Tooltip("Reference to the servert socket controller")]
    private EGS_ServerSocket socketController;

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
    /// <param name="socketController_">Reference to the server socket controller</param>
    /// <param name="client_socket_">Reference to the client socket assigned</param>
    /// <param name="clientType_">Type of client</param>
    public EGS_RoundTripTime(EGS_ServerSocket socketController_, Socket client_socket_, EGS_Control.EGS_Type clientType_)
    {
        // Assign references.
        socketController = socketController_;
        client_socket = client_socket_;
        clientType = clientType_;

        // Initialize variables.
        roundTripTimeStopwatch = new Stopwatch();
        roundTripTimeThread = new Thread(() => RTT());
        stillConnected = true;

        // Start the timeout timer and the RTT.
        StartTimeoutTimer();
        StartRTT();
    }
    #endregion

    #region Class Methods
    #region Public Methods
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
    #endregion

    #region Private Methods
    /// <summary>
    /// Method StartTimeoutTimer, that sets and starts a timer for the disconnection.
    /// </summary>
    private void StartTimeoutTimer() {
        timeoutTimer = new System.Timers.Timer(EGS_Config.DISCONNECT_TIMEOUT);
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
        while (stillConnected)
        {
            connectedMutex.WaitOne();

            // Checking that still connected to be sure it didn't change before the mutex.
            if (!stillConnected)
                return;

            EGS_Message msg = new EGS_Message();
            msg.messageType = "RTT";
            msg.messageContent = lastRTTMilliseconds.ToString();
            string jsonMSG = msg.ConvertMessage();

            try
            {
                if (client_socket.Connected)
                    socketController.Send(client_socket, jsonMSG);
                // TODO: BUG: If this fails, client_socket loses its RemoteEndPoint.
            }
            finally
            {
                connectedMutex.ReleaseMutex();
            }
            /*catch (SocketException se)
            {
                
                // TODO: Control this exception.
            }*/

            if (!roundTripTimeStopwatch.IsRunning)
                roundTripTimeStopwatch.Start();

            Thread.Sleep(EGS_Config.TIME_BETWEEN_RTTS);
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
    #endregion
}
