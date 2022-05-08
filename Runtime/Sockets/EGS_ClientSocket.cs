using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class EGS_ClientSocket, that controls a client sender socket.
/// </summary>
public class EGS_ClientSocket
{
    #region Variables
    [Header("ManualResetEvents")]
    [Tooltip("ManualResetEvent for when connection is done")]
    protected ManualResetEvent connectDone = new ManualResetEvent(false);
    [Tooltip("ManualResetEvent for when send is done.")]
    protected ManualResetEvent sendDone = new ManualResetEvent(false);
    [Tooltip("ManualResetEvent for when receive is done")]
    protected ManualResetEvent receiveDone = new ManualResetEvent(false);

    // Since a client can only be connected to ONE server, it will never overwrite data.
    // It is needed because data can be splitted among messages.
    [Tooltip("Response from the remote device")]
    private string response = string.Empty;
    #endregion

    #region Class Methods
    #region Public Methods
    /// <summary>
    /// Method StartClient, that tries to connect to the server.
    /// </summary>
    /// <param name="remoteEP">EndPoint where the server is</param>
    /// <param name="socket_client">Client socket to use</param>
    public void StartClient(EndPoint remoteEP, Socket socket_client)
    {
        // Reset the ManualResetEvents.
        connectDone.Reset();
        sendDone.Reset();
        receiveDone.Reset();

        // Connect to a remote device.
        try
        {
            // Connect to the remote endpoint.  
            socket_client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), socket_client);

            // Wait until the connection is done.
            connectDone.WaitOne();

            // Receive the response from the remote device.  
            Receive(socket_client);
        }
        catch (ThreadInterruptedException)
        {
            // Log.
            Debug.LogWarning("Interruped connections thread.");
        }
        catch (Exception e)
        {
            // Log.
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method ConnectCallback, called when connected to server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public virtual void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);

            // LOG.
            Debug.Log("[CLIENT] Socket connected to " +
                client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    /// <summary>
    /// Method Receive, to receive data from server.
    /// </summary>
    /// <param name="client">Client Socket</param>
    public void Receive(Socket client)
    {
        try
        {
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = client;

            if (client.Connected)
            {
                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);

                // Wait until the receive is done.
                receiveDone.WaitOne();
            }
        }
        catch (Exception e)
        {
            // LOG.
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method ReceiveCallback, called when received data from server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    protected virtual void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the remote device.  
            int bytesRead = 0;

            if (handler.Connected)
            {
                try
                {
                    bytesRead = handler.EndReceive(ar);
                }
                catch (ObjectDisposedException)
                {
                    // LOG. Object already disposed...
                }
            }

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                if (state.sb.ToString().EndsWith("<EOM>"))
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 0)
                    {
                        response = state.sb.ToString();
                    }

                    // Signal that all bytes have been received.  
                    receiveDone.Set();

                    // Keep receiving messages from the server.
                    Receive(handler);

                    // Split if there is more than one message
                    string[] receivedMessages = response.Split(new string[] { "<EOM>" }, StringSplitOptions.None);

                    // Handle the messages (split should leave one empty message at the end so we skip it by substract - 1 to the length)
                    for (int i = 0; i < (receivedMessages.Length - 1); i++)
                        HandleMessage(receivedMessages[i], handler);
                }
                else
                {
                    // Get the rest of the data.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
            }
        }
        catch (ThreadInterruptedException)
        {
            // LOG.
            // TODO: Control this exception.
        }
        catch (Exception e)
        {
            // LOG.
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method Send, for send a message to the server.
    /// </summary>
    /// <param name="client">Client Socket</param>
    /// <param name="data">String with the data to send</param>
    public void Send(Socket client, string data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);

        // Wait until the send is done.
        sendDone.WaitOne();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method SendCallback, called when sent data to server.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    protected void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            if (EGS_Config.DEBUG_MODE_CONSOLE >= EGS_Control.EGS_DebugLevel.Complete)
                Debug.Log("[CLIENT] Sent " + bytesSent + " bytes to server.");

            // LOG.

            // Signal that all bytes have been sent.  
            sendDone.Set();
        }
        catch (Exception e)
        {
            // LOG.
            Debug.LogError("[CLIENT] " + e.ToString());
        }
    }

    /// <summary>
    /// Method HandleMessage, that receives a message from the server or game server and do things based on it.
    /// </summary>
    /// <param name="content">Message content</param>
    /// <param name="handler">Socket that handles that connection</param>
    protected virtual void HandleMessage(string content, Socket handler)
    {
       
    }

    #region MainThreadFunctions
    /// <summary>
    /// Method LoadScene, that loads a scene in the main thread.
    /// </summary>
    /// <param name="sceneName">Scene name</param>
    protected void LoadScene(string sceneName)
    {
        EGS_Dispatcher.RunOnMainThread(() => { SceneManager.LoadScene(sceneName); });
    }
    #endregion
    #endregion
    #endregion
}