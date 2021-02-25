using System.Threading;

public class EGS_Sockets
{
    #region Variables
    /// Threads
    // Thread that runs the server.
    private Thread serverThread;

    // Thread that runs the client (just for testing purposes, probably unnecesary).
    private Thread clientThread;

    ///  References
    // Reference to the Log.
    private EGS_Log egs_Log = null;
    #endregion

    #region Constructors
    /// <summary>
    /// Main constructor that assigns the log.
    /// </summary>
    /// <param name="log">Log instance</param>
    public EGS_Sockets(EGS_Log log)
    {
        egs_Log = log;
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, that calls a thread for the server.
    /// </summary>
    /// 
    public void StartListening(string serverIP, int serverPort)
    {
        serverThread = new Thread(() => EGS_SE_SocketListener.StartListening(serverIP, serverPort, egs_Log));
        serverThread.Start();
    }

    /// <summary>
    /// Method StartClient, that calls a thread for the client (just for testing purposes, probably unnecesary).
    /// </summary>
    /// <param name="serverIP">IP where server is</param>
    /// <param name="serverPort">Port where server is</param>
    public void StartClient(string serverIP, int serverPort)
    {
        clientThread = new Thread(() => EGS_CL_SocketClient.StartClient(serverIP, serverPort));
        clientThread.Start();
    }

    /// <summary>
    /// Method StopListening, that stops the threads and closes the server socket.
    /// </summary>
    public void StopListening()
    {
        // Stop client thread and wait (provisional).
        clientThread.Abort();
        clientThread.Join();

        // Stop server thread and wait.
        serverThread.Abort();
        serverThread.Join();
        EGS_SE_SocketListener.StopListening();
    }
    #endregion
}
