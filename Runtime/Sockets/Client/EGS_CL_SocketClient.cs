using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Class EGS_CL_SocketClient, that controls the client sender socket.
/// </summary>
public class EGS_CL_SocketClient
{
    #region Variables
    /// ManualResetEvents
    // ManualResetEvent for when connection is done.
    private static ManualResetEvent connectDone = new ManualResetEvent(false);
    // ManualResetEvent for when send is done.
    private static ManualResetEvent sendDone = new ManualResetEvent(false);
    // ManualResetEvent for when receive is done.
    private static ManualResetEvent receiveDone = new ManualResetEvent(false);

    /// Server data
    // Server IP.
    private static string serverIP;
    // Server Port.
    private static int serverPort;

    /// Sockets
    // Client socket.
    private static Socket socket_client;

    /// Other
    // Response from the remote device.
    private static String response = String.Empty;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_CL_SocketClient()
    {
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartClient, that tries to connect to the server.
    /// </summary>
    /// <param name="serverIP">IP where server is</param>
    /// <param name="serverPort">Port where server is</param>
    public static void StartClient(string serverIP_, int serverPort_)
    {
        // Assign data
        serverIP = serverIP_;
        serverPort = serverPort_;

        // Connect to a remote device.
        try
        {
            // Obtain IP direction and endpoint
            IPHostEntry ipHostInfo = Dns.GetHostEntry(serverIP);
            // It is IPv4, for IPv6 it would be 0.
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, serverPort);

            // Create a TCP/IP socket
            socket_client = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            socket_client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), socket_client);
            connectDone.WaitOne();

            // Send test data to the remote device.  
            Send(socket_client, "This is a test<EOF>");
            sendDone.WaitOne();

            // Receive the response from the remote device.  
            Receive(socket_client);
            receiveDone.WaitOne();

            // Write the response to the console.  
            Debug.Log("[CLIENT] Response received : " + response);

            // Release the socket.  
            socket_client.Shutdown(SocketShutdown.Both);
            socket_client.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method ConnectCallback, called when connected to server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            Debug.Log("[CLIENT] Socket connected to " +
                client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    /// <summary>
    /// Method Receive, to receive data from server.
    /// </summary>
    /// <param name="client">Socket</param>
    private static void Receive(Socket client)
    {
        try
        {
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = client;

            // Begin receiving the data from the remote device.  
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method ReceiveCallback, called when received data from server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.  
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Get the rest of the data.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                // All the data has arrived; put it in response.  
                if (state.sb.Length > 1)
                {
                    response = state.sb.ToString();
                }
                // Signal that all bytes have been received.  
                receiveDone.Set();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method Send, for send a message to the server.
    /// </summary>
    /// <param name="client">Socket</param>
    /// <param name="data">String with the data to send</param>
    private static void Send(Socket client, String data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
    }

    /// <summary>
    /// Method SendCallback, called when sent data to server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            Debug.Log("[CLIENT] Sent " + bytesSent + " bytes to server.");

            // Signal that all bytes have been sent.  
            sendDone.Set();
        }
        catch (Exception e)
        {
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }
    #endregion
}