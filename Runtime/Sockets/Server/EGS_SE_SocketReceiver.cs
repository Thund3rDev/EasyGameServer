using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class EGS_SE_SocketReceiver : MonoBehaviour
{
    #region Variables
    [Header("Sockets")]
    [Tooltip("Socket receiver")]
    private Socket socket_receiver = new Socket(AddressFamily.InterNetwork,
        SocketType.Stream, ProtocolType.Tcp);

    [Header("References")]
    [Tooltip("Reference to the Log")]
    [SerializeField]
    private EGS_Log egs_Log = null;
    #endregion

    public void StartServer()
    {
        // Obtener dirección IP y endpoint
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        // Es IPv4, para IPv6 sería la 0
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        socket_receiver.Bind(localEndPoint);
        socket_receiver.Listen(100);

        egs_Log.Log("<color=green>Easy Game Server</color> Listening at port <color=orange>11000</color>.");
    }

    /*// Update is called once per frame
    void Update()
    {
        
    }*/
}
