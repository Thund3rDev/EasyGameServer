using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Class EGS_SE_Sockets, that will control server sockets in the future.
/// </summary>
public class EGS_SE_Sockets
{
    #region Variables
    // Thread that runs the server.
    private Thread serverThread;

    // Server socket.
    private Socket socket_listener;

    /// Server data
    // Server IP.
    private string serverIP;
    // Server Port.
    private int serverPort;

    /// References
    // Reference to the Log.
    private EGS_Log egs_Log = null;
    #endregion

    #region Constructors
    /// <summary>
    /// Main constructor that assigns the log.
    /// </summary>
    /// <param name="log_">Log instance</param>
    /// <param name="serverIP_">IP where server will be set</param>
    /// <param name="serverPort_">Port where server will be set</param>
    public EGS_SE_Sockets(EGS_Log log_, string serverIP_, int serverPort_)
    {
        egs_Log = log_;
        serverIP = serverIP_;
        serverPort = serverPort_;
    }
    #endregion

    /// <summary>
    /// Method StartServer, to start listening on the server.
    /// </summary>
    public void StartServer()
    {
        // Create socket and get EndPoint
        EndPoint localEP = CreateSocket(serverIP, serverPort);

        // Connect to server
        EGS_SE_SocketListener serverSocketHandler = new EGS_SE_SocketListener(egs_Log);
        serverThread = new Thread(() => serverSocketHandler.StartListening(serverPort, localEP, socket_listener));
        serverThread.Start();
    }

    #region Private Methods
    /// <summary>
    /// Method CreateSocket, that creates the client socket and returns the server endpoint
    /// </summary>
    /// <param name="serverIP">IP where server will be set</param>
    /// <param name="serverPort">Port where server will be set</param>
    /// <returns>EndPoint where the server it is</returns>
    private EndPoint CreateSocket(string serverIP, int serverPort)
    {
        // Obtain IP direction and endpoint.
        IPHostEntry ipHostInfo = Dns.GetHostEntry(serverIP);
        // It is IPv4, for IPv6 it would be 0.
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, serverPort);

        // Create a TCP/IP socket.
        socket_listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Return the EndPoint
        return localEndPoint;
    }

    /// <summary>
    /// Method StopListening, to close the socket and stop listening to connections.
    /// </summary>
    public void StopListening()
    {
        serverThread.Abort();
        serverThread.Join();
        socket_listener.Close();
        egs_Log.Log("<color=green>Easy Game Server</color> stopped listening at port <color=orange>" + serverPort + "</color>.");
    }

    /*/// <summary>
    /// Method ReadCallback, called when a client sends a message.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public void ReadCallback(IAsyncResult ar)
    {
        string content = string.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There might be more data, so store the data received so far.  
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Read message data.
            content = state.sb.ToString();

            // Handle the message
            HandleMessage(content, handler);
        }
    }

    private void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        EGS_Message receivedMessage = new EGS_Message();
        receivedMessage = JsonUtility.FromJson<EGS_Message>(content);

        // Depending on the messageType, do different things
        switch (receivedMessage.messageType)
        {
            case "connect":
                // Get the received user
                EGS_User receivedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);

                // Display data on the console.  
                egs_Log.Log("Read " + content.Length + " bytes from socket. \n<color=purple>Data:</color> UserID: " + receivedUser.userID + " - Username: " + receivedUser.username);

                // Echo the data back to the client.
                string messageToSend = "Welcome, " + receivedUser.username;
                Send(handler, messageToSend);
                break;
            case "test_message":
                // Display data on the console.  
                egs_Log.Log("Read " + content.Length + " bytes from socket. \n<color=purple>Data:</color>" + receivedMessage.messageContent);
                break;
            default:
                break;
        }
    }*/
    #endregion
}