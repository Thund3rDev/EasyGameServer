using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class EGS_CL_SocketSender : MonoBehaviour
{
    #region Variables
    [Header("Sockets")]
    [Tooltip("Socket")]
    private Socket socket = new Socket(AddressFamily.InterNetwork,
        SocketType.Stream, ProtocolType.Tcp);

    [Header("References")]
    [Tooltip("Reference to the Log")]
    [SerializeField]
    private EGS_Log egs_Log = null;
    #endregion

    public void StartClient()
    {
        // Obtener dirección IP y endpoint
        IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
        // Es IPv4, para IPv6 sería la 0
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        IPEndPoint ipe = new IPEndPoint(ipAddress, 11000);

        try
        {
            socket.Connect(ipe);
            egs_Log.Log("<color=blue>Client</color> connected.");
        }
        catch (ArgumentNullException ae)
        {
            Debug.Log("ArgumentNullException : " + ae.ToString());
        }
        catch (SocketException se)
        {
            Debug.Log("SocketException : " + se.ToString());
        }
        catch (Exception e)
        {
            Debug.Log("Unexpected exception : " + e.ToString());
        }
    }

    /*// Update is called once per frame
    void Update()
    {
        
    }*/
}
