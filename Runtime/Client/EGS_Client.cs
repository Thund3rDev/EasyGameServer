using System.IO;
using System.Net.Sockets;
using System.Xml;
using UnityEngine;

/// <summary>
/// Class EGS_Client, that controls the client side.
/// </summary>
public class EGS_Client : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_Client client_instance;

    [Tooltip("Struct that contains the server data")]
    public static EGS_ServerData serverData;

    [Header("Networking")]
    [Tooltip("Bool that indicates if client is connnected to the server")]
    public static bool connectedToServer = false;

    [Tooltip("Controller for client socket")]
    private EGS_CL_Sockets clientSocketController = null;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (client_instance == null)
        {
            client_instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
            Destroy(this.gameObject);

        
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method ConnectToServer, that prepares all client data and try to connect to the server.
    /// </summary>
    public void ConnectToServer()
    {
        // Check if server already started.
        if (connectedToServer)
        {
            Debug.LogWarning("Client already connected.");
            return;
        }

        // Establish the connection to the server.
        connectedToServer = true;

        // Read server config data.
        ReadServerData();

        // Create client socket manager
        clientSocketController = new EGS_CL_Sockets();
        // Connect to the server
        clientSocketController.ConnectToServer();
    }

    /// <summary>
    /// Method DisconnectFromServer, that will stop sending messages and listening.
    /// </summary>
    public void DisconnectFromServer()
    {
        // If not connected to server, return.
        if (!connectedToServer)
            return;

        // Disconnect from server.
        connectedToServer = false;

        // Stop listening on the socket.
        clientSocketController.Disconnect();
    }

    /// <summary>
    /// Method SendMessage, that will send a message to the server
    /// </summary>
    /// <param name="type">String that contains the type of the message</param>
    /// <param name="msg">String that contains the message itself</param>
    public void SendMessage(string type, string msg)
    {
        // Send the message by the socket controller.
        clientSocketController.SendMessage(type, msg);
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Method ReadServerData, to load all server config data.
    /// </summary>
    private void ReadServerData()
    {
        // Read server config data.
        string configXMLPath = "Packages/com.thund3r.easy_game_server/config.xml";
        if (!File.Exists("Packages/com.thund3r.easy_game_server/config.xml"))
            configXMLPath = "./config.xml";

        XmlDocument doc = new XmlDocument();
        doc.Load(configXMLPath);

        XmlNode node;

        // Get server version.
        node = doc.DocumentElement.SelectSingleNode("//version");
        serverData.version = node.InnerText;

        // Get server ip.
        node = doc.DocumentElement.SelectSingleNode("//server-ip");
        serverData.serverIP = node.InnerText;

        // Get server port.
        node = doc.DocumentElement.SelectSingleNode("//port");
        serverData.serverPort = int.Parse(node.InnerText);
    }
    #endregion
}
