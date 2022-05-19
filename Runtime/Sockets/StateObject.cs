using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// State object for receiving data from remote device.
/// </summary>
public class StateObject
{
    [Header("StateObject")]
    [Tooltip("Size of receive buffer")]
    public const int BufferSize = 1024;

    [Tooltip("Receive buffer")]
    public byte[] buffer = new byte[BufferSize];

    [Tooltip("Received data string")]
    public StringBuilder sb = new StringBuilder();

    [Tooltip("Client socket")]
    public Socket workSocket = null;
}
