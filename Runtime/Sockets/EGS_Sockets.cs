using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// State object for receiving data from remote device.
public class StateObject
{
    // Size of receive buffer.  
    public const int BufferSize = 1024;

    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];

    // Received data string.
    public StringBuilder sb = new StringBuilder();

    // Client socket.
    public Socket workSocket = null;
}

public class EGS_Sockets
{
    #region Variables
    [Header("Tasks")]
    [Tooltip("Task that runs the server")]
    private Task serverTask;

    [Tooltip("Task that runs the client")]
    private Task clientTask;

    [Header("References")]
    [Tooltip("Reference to the Log")]
    [SerializeField]
    private EGS_Log egs_Log = null;

    /*[Tooltip("Reference to the Sockets server controller")]
    [SerializeField]
    private EGS_SE_Sockets egs_SE_sockets = null;

    [Tooltip("Reference to the Sockets client controller")]
    [SerializeField]
    private EGS_CL_Sockets egs_CL_sockets = null;*/
    #endregion

    #region Constructors
    public EGS_Sockets(EGS_Log log)
    {
        egs_Log = log;
    }
    #endregion

    #region Class Methods
    public void StartListening(string serverIP, int serverPort)
    {
        serverTask = Task.Run(() => EGS_SE_SocketListener.StartListening(serverIP, serverPort, egs_Log));
        egs_Log.Log("Called to run server task");
    }

    public void StartClient(string serverIP, int serverPort)
    {
        clientTask = Task.Run(() => EGS_CL_SocketClient.StartClient(serverIP, serverPort, egs_Log));
        egs_Log.Log("Called to run client task");
    }

    public void StopListening(int serverPort)
    {
        egs_Log.Log("<color=green>Easy Game Server</color> stopped listening at port <color=orange>" + serverPort + "</color>.");
    }
    #endregion

    #region Private Methods
    #endregion
}
