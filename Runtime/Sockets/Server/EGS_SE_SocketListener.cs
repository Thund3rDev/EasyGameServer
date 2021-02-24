using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_SE_SocketListener, that controls the server receiver socket.
/// </summary>
public class EGS_SE_SocketListener
{ 
    #region Variables
    /// Concurrency
    // Thread signal.
    public static ManualResetEvent allDone = new ManualResetEvent(false);

    /// Server data
    // Server IP.
    private static string serverIP;
    // Server Port.
    private static int serverPort;

    /// Sockets
    // Socket listener.
    private static Socket socket_listener;

    /// References
    // Reference to the Log.
    private static EGS_Log egs_Log = null;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_SE_SocketListener()
    {
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, that opens the socket to connections.
    /// </summary>
    /// <param name="serverIP_">IP where server will be set</param>
    /// <param name="serverPort_">Port where server will be set</param>
    /// <param name="log">Log instance</param>
    public static void StartListening(string serverIP_, int serverPort_, EGS_Log log)
    {
        // Assign data.
        serverIP = serverIP_;
        serverPort = serverPort_;
        egs_Log = log;

        // Obtain IP direction and endpoint.
        IPHostEntry ipHostInfo = Dns.GetHostEntry(serverIP);
        // It is IPv4, for IPv6 it would be 0.
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, serverPort);

        // Create a TCP/IP socket.
        socket_listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            socket_listener.Bind(localEndPoint);
            socket_listener.Listen(100);

            egs_Log.Log("<color=green>Easy Game Server</color> Listening at port <color=orange>" + serverPort + "</color>.");

            while (true)
            { 
                // Set the event to nonsignaled state.  
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.  
                egs_Log.Log("Waiting for a connection...");
                socket_listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    socket_listener);

                // Wait until a connection is made before continuing.  
                allDone.WaitOne();

                egs_Log.Log("<color=blue>Client</color> connected.");
            }
        }
        catch(ThreadAbortException)
        {
            //egs_Log.LogWarning("Aborted server thread");
        }
        catch (Exception e)
        {  
            egs_Log.LogError(e.ToString());
        } 

    }

    /// <summary>
    /// Method AcceptCallback, called when a client connects to the server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.  
        allDone.Set();

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    /// <summary>
    /// Method ReadCallback, called when a client sends a message.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public static void ReadCallback(IAsyncResult ar)
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

            // Check for end-of-file tag. If it is not there, read
            // more data.  
            content = state.sb.ToString();
            if (content.IndexOf("<EOF>") > -1)
            {
                // All the data has been read from the
                // client. Display it on the console.  
                egs_Log.Log("Read " + content.Length + " bytes from socket. \n Data : " + content);

                // Echo the data back to the client.  
                Send(handler, content);
            }
            else
            {
                // Not all data received. Get more.  
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            }
        }
    }

    /// <summary>
    /// Method Send, to send a message to a client.
    /// </summary>
    /// <param name="handler">Socket</param>
    /// <param name="data">String that contains the data to send</param>
    private static void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    /// <summary>
    /// Method SendCallback, called when a message was sent.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            egs_Log.Log("Sent " + bytesSent + " bytes to client.");

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        }
        catch (Exception e)
        {
            egs_Log.LogError(e.ToString());
        }
    }

    /// <summary>
    /// Method StopListening, to close the socket and stop listening to connections.
    /// </summary>
    public static void StopListening()
    {
        socket_listener.Close();
        egs_Log.Log("<color=green>Easy Game Server</color> stopped listening at port <color=orange>" + serverPort + "</color>.");
    }
    #endregion
}