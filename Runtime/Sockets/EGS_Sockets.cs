using System.Threading;

public class EGS_Sockets
{
    #region Variables
    /// Controllers
    // Controller for client sockets.
    private EGS_CL_Sockets clientSocketsController;
    // Controller for server sockets.
    private EGS_SE_Sockets serverSocketsController;

    /// References
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
        serverSocketsController = new EGS_SE_Sockets(egs_Log, serverIP, serverPort);
        serverSocketsController.StartServer();
    }

    /// <summary>
    /// Method StartClient, that calls a thread for the client (just for testing purposes, probably unnecesary).
    /// </summary>
    /// <param name="serverIP">IP where server is</param>
    /// <param name="serverPort">Port where server is</param>
    public void StartClient(string serverIP, int serverPort)
    {
        clientSocketsController = new EGS_CL_Sockets(egs_Log);
        clientSocketsController.ConnectToServer(serverIP, serverPort);
    }

    /// <summary>
    /// Method StopListening, that stops the threads and closes the server socket.
    /// </summary>
    public void StopListening()
    {
        // Disconnect client (provisional).
        clientSocketsController.Disconnect();

        // Stop listening on server.
        serverSocketsController.StopListening();
    }
    #endregion
}
