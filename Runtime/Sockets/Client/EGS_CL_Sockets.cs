using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_CL_Sockets, that controls client sockets.
/// </summary>
public class EGS_CL_Sockets
{
    #region Variables
    // Thread that runs the client (just for testing purposes, probably unnecesary).
    private Thread clientThread;

    // Client socket.
    private Socket socket_client;

    // Boolean that indicates if client is connected to the server.
    public static bool connectedToServer;
    #endregion

    #region Constructors
    /// <summary>
    /// Main constructor.
    /// </summary>
    public EGS_CL_Sockets()
    {
        connectedToServer = false;
    }
    #endregion

    /// <summary>
    /// Method ConnectToServer, to establish a connection to the server.
    /// </summary>
    /// <param name="serverIP">IP where server will be set</param>
    /// <param name="serverPort">Port where server will be set</param>
    public void ConnectToServer(string serverIP, int serverPort)
    {
        // Create socket and get EndPoint
        EndPoint remoteEP = CreateSocket(serverIP, serverPort);

        // Connect to server
        EGS_CL_SocketClient clientSocketHandler = new EGS_CL_SocketClient();
        clientThread = new Thread(() => clientSocketHandler.StartClient(remoteEP, socket_client));
        clientThread.Start();
    }

    public void SendMessage(string type, string msg)
    {
        // Create new message.
        EGS_Message thisMessage = new EGS_Message();
        thisMessage.messageType = type;
        thisMessage.messageContent = msg;

        // Convert message to JSON .
        string messageJson = JsonUtility.ToJson(thisMessage);

        // Send the message.
        Send(messageJson);
    }

    /// <summary>
    /// Method Send, for send a message to the server.
    /// </summary>
    /// <param name="data">string with the data to send</param>
    private void Send(string data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        socket_client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), socket_client);
    }

    /// <summary>
    /// Method SendCallback, called when sent data to server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            Debug.Log("[CLIENT] Sent " + bytesSent + " bytes to server.");
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method Disconnect, to stop the client thread and disconnect from server.
    /// </summary>
    public void Disconnect()
    {
        clientThread.Abort();
        clientThread.Join();
        CloseSocket();
        connectedToServer = false;
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
        // Obtain IP direction and endpoint
        IPHostEntry ipHostInfo = Dns.GetHostEntry(serverIP);
        // It is IPv4, but if wifi is using, it should be 1 and not 0.
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

        // Create a TCP/IP socket
        socket_client = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Return the EndPoint
        return remoteEP;
    }

    /// <summary>
    /// Method CloseSocket, to close the client socket.
    /// </summary>
    private void CloseSocket()
    {
        socket_client.Shutdown(SocketShutdown.Both);
        socket_client.Close();

        Debug.Log("[CLIENT] Closed socket.");
    }
    #endregion
}
