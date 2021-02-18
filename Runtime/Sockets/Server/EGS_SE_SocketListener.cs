using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class EGS_SE_SocketListener : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Port where the server is")]
    private int serverPort = -1;

    [Header("Sockets")]
    [Tooltip("Socket listener")]
    private Socket socket_listener = new Socket(AddressFamily.InterNetwork,
        SocketType.Stream, ProtocolType.Tcp);

    [Header("References")]
    [Tooltip("Reference to the Log")]
    [SerializeField]
    private EGS_Log egs_Log = null;
    #endregion

    public void StartListening(string serverIP, int serverPort_)
    {
        // Assign the server port
        serverPort = serverPort_;

        // Obtain IP direction and endpoint
        IPHostEntry ipHostInfo = Dns.GetHostEntry(serverIP);
        // It is IPv4, for IPv6 it would be 0.
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, serverPort);

        socket_listener.Bind(localEndPoint);
        socket_listener.Listen(100);

        egs_Log.Log("<color=green>Easy Game Server</color> Listening at port <color=orange>" + serverPort + "</color>.");
    }

    /// <summary>
    /// Method StopListening, that stop the server from listen more.
    /// </summary>
    public void StopListening()
    {
        socket_listener.Close();
        egs_Log.Log("<color=green>Easy Game Server</color> stopped listening at port <color=orange>" + serverPort + "</color>.");
    }

    /*// Update is called once per frame
    void Update()
    {
        
    }*/
}
