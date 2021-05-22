using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Timers;
using System.Linq;

/// <summary>
/// Class EGS_SE_SocketController, that controls the server receiver socket.
/// </summary>
public class EGS_SE_SocketController
{
    #region Variables
    /// ManualResetEvents
    // ManualResetEvent allDone for connection.
    public ManualResetEvent allDone = new ManualResetEvent(false);

    /// References
    // Reference to the Log.
    private EGS_Log egs_Log = null;

    /// Delegates
    // Delegate to the AfterClientConnected function.
    private Action<Socket> onConnectDelegate;
    // Delegate to the OnClientDisconnect function.
    private Action<Socket> onDisconnectDelegate;

    // List of timers.
    private Dictionary<Socket, System.Timers.Timer> socketTimeoutCounters = new Dictionary<Socket, System.Timers.Timer>();

    // Connected users.
    private Dictionary<Socket, EGS_User> connectedUsers = new Dictionary<Socket, EGS_User>();
    // Players in game.
    private Dictionary<string, EGS_Player> playersInGame = new Dictionary<string, EGS_Player>();

    #endregion

    #region Constructors
    /// <summary>
    /// Empty constructor.
    /// </summary>
    public EGS_SE_SocketController(EGS_Log log, Action<Socket> afterClientConnected, Action<Socket> onClientDisconnect)
    {
        egs_Log = log;
        onConnectDelegate = afterClientConnected;
        onDisconnectDelegate = onClientDisconnect;
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method StartListening, that opens the socket to connections.
    /// </summary>
    /// <param name="serverPort">Port where the server is</param>
    /// <param name="remoteEP">EndPoint where the server is</param>
    /// <param name="socket_listener">Socket to use</param>
    public void StartListening(int serverPort, EndPoint localEP, Socket socket_listener)
    {
        // Bind the socket to the local endpoint and listen for incoming connections.  
        try
        {
            socket_listener.Bind(localEP);
            socket_listener.Listen(100);

            egs_Log.Log("<color=green>Easy Game Server</color> Listening at port <color=orange>" + serverPort + "</color>.");

            // Start listening for connections asynchronously.
            socket_listener.BeginAccept(
                new AsyncCallback(AcceptCallback),
                socket_listener);
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
    public void AcceptCallback(IAsyncResult ar)
    {
        allDone.Set();

        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Do things on client connected.
        onConnectDelegate(handler);

        // Put a heartbeat for the client socket.
        HeartbeatClient(handler);

        // Create the state object and begin receive.  
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);

        // Start listening for connections asynchronously.
        listener.BeginAccept(
            new AsyncCallback(AcceptCallback),
            listener);
    }

    /// <summary>
    /// Method ReadCallback, called when a client sends a message.
    /// </summary>
    /// <param name="ar">IAsyncResult</param>
    public void ReadCallback(IAsyncResult ar)
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

            // Read message data.
            content = state.sb.ToString();

            // Split if there is more than one message
            string[] receivedMessages = content.Split(new string[] { "<EOM>" }, StringSplitOptions.None);

            // Handle the messages (split should leave one empty message at the end so we skip it by substract - 1 to the length)
            for (int i = 0; i < (receivedMessages.Length - 1); i++)
                HandleMessage(receivedMessages[i], handler);

            // Keep receiving for that socket.
            state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);

            if (EGS_ServerManager.DEBUG_MODE > 1)
                egs_Log.Log("Keep receiving messages from: " + handler.RemoteEndPoint);
        }
    }

    /// <summary>
    /// Method Send, to send a message to a client.
    /// </summary>
    /// <param name="handler">Socket</param>
    /// <param name="data">String that contains the data to send</param>
    public void Send(Socket handler, string data)
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
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = handler.EndSend(ar);
            if (EGS_ServerManager.DEBUG_MODE > 1)
                egs_Log.Log("Sent " + bytesSent + " bytes to client.");

        }
        catch (SocketException) { }
        catch (Exception e)
        {
            egs_Log.LogError(e.ToString());
        }
    }

    private void HandleMessage(string content, Socket handler)
    {
        // Read data from JSON.
        EGS_Message receivedMessage = new EGS_Message();
        receivedMessage = JsonUtility.FromJson<EGS_Message>(content);

        if (EGS_ServerManager.DEBUG_MODE > 1)
            egs_Log.Log("Read " + content.Length + " bytes from socket - " + handler.RemoteEndPoint +
            " - Message type: " + receivedMessage.messageType);

        // Message to send back.
        EGS_Message msg = new EGS_Message();

        string jsonMSG;
        EGS_User receivedUser;

        // Depending on the messageType, do different things
        switch (receivedMessage.messageType)
        {
            case "TEST_MESSAGE":
                // Display data on the console.  
                egs_Log.Log("<color=purple>Data:</color> " + receivedMessage.messageContent);
                break;
            case "KEEP_ALIVE":
                socketTimeoutCounters[handler].Stop();
                socketTimeoutCounters[handler].Start();
                break;
            case "JOIN_SERVER":
                // Get the received user
                receivedUser = JsonUtility.FromJson<EGS_User>(receivedMessage.messageContent);
                receivedUser.SetSocket(handler);

                // Display data on the console.
                egs_Log.Log("<color=purple>Data:</color> UserID: " + receivedUser.GetUserID() + " - Username: " + receivedUser.GetUsername());

                // Save user data on the dictionary of connectedUsers.
                connectedUsers.Add(handler, receivedUser);

                // Echo the data back to the client.
                msg.messageType = "JOIN_SERVER";
                msg.messageContent = "Welcome, " + receivedUser.GetUsername();
                jsonMSG = msg.ConvertMessage();

                Send(handler, jsonMSG);
                break;
            case "QUEUE_JOIN":
                // Get the user
                EGS_User thisUser = connectedUsers[handler];

                // Add the player to the queue.
                EGS_Player newPlayer = new EGS_Player(thisUser);
                EGS_GamesManager.gm_instance.searchingGame_players.Enqueue(newPlayer);

                if (EGS_ServerManager.DEBUG_MODE > 0)
                    egs_Log.Log("Searching game: " + thisUser.GetUsername());

                CheckQueueToStartGame();
                break;
            case "QUEUE_LEAVE":
                bool isPlayerInQueue = false;

                // Lock the queue
                lock (EGS_GamesManager.gm_instance.searchingGame_players)
                {
                    // Check if player is in queue.
                    foreach (EGS_Player p in EGS_GamesManager.gm_instance.searchingGame_players)
                    {
                        if (p.GetUser().GetSocket() == handler)
                        {
                            isPlayerInQueue = true;
                            break;
                        }
                    }

                    if (isPlayerInQueue)
                    {
                        // Remove the player from the Queue by constructing a new queue based on the previous one but without the left player.
                        EGS_GamesManager.gm_instance.searchingGame_players =
                            new ConcurrentQueue<EGS_Player>(EGS_GamesManager.gm_instance.searchingGame_players.Where(x => x.GetUser().GetSocket() != handler));
                    }
                }
                break;
            case "READY":
                int roomReady = int.Parse(receivedMessage.messageContent);
                bool startedGame = EGS_GamesManager.gm_instance.Ready(roomReady);

                if (startedGame)
                {
                    msg = new EGS_Message();
                    msg.messageType = "GAME_START";
                    msg.messageContent = "";

                    jsonMSG = msg.ConvertMessage();

                    foreach (EGS_Player p in EGS_GamesManager.gm_instance.games[roomReady].Game.GetPlayers())
                    {
                        Send(p.GetUser().GetSocket(), jsonMSG);
                    }
                }
                break;
            case "INPUT":
                // Get the input data
                // Inputs[0] = userName | Inputs[1-4] = directions.
                string[] inputs = receivedMessage.messageContent.Split(',');

                bool[] realInputs = new bool[4];
                for (int i = 0; i < realInputs.Length; i++)
                    realInputs[i] = bool.Parse(inputs[i + 1]);

                // Get the player from its username.
                EGS_Player thisPlayer = playersInGame[inputs[0]];

                // Assign its inputs.
                thisPlayer.SetInputs(realInputs);
                break;
            default:
                egs_Log.Log("<color=yellow>Undefined message type: </color>" + receivedMessage.messageType);
                break;
        }
    }

    private void CheckQueueToStartGame()
    {
        bool areEnoughForAGame = false;
        List<EGS_Player> playersForThisGame = new List<EGS_Player>();

        // Lock to evit problems with the queue.
        lock (EGS_GamesManager.gm_instance.searchingGame_players)
        {
            // If there are enough players to start a game.
            if (EGS_GamesManager.gm_instance.searchingGame_players.Count >= EGS_GamesManager.gm_instance.PLAYERS_PER_GAME)
            {
                areEnoughForAGame = true;
                for (int i = 0; i < EGS_GamesManager.gm_instance.PLAYERS_PER_GAME; i++)
                {
                    // Get the player from the queue.
                    EGS_Player playerToGame;
                    EGS_GamesManager.gm_instance.searchingGame_players.TryDequeue(out playerToGame);

                    // Add the player to the dictionary and the list of this game.
                    playersInGame.Add(playerToGame.GetUser().GetUsername(), playerToGame);
                    playersForThisGame.Add(playerToGame);
                }
            }
        }

        // If there are enough players for a game:
        if (areEnoughForAGame)
        {
            // Create the game and get the room number.
            int room = EGS_GamesManager.gm_instance.CreateGame(this, playersForThisGame);

            // Construct the message to send.
            EGS_UpdateData updateData = new EGS_UpdateData();
            updateData.SetRoom(room);

            for (int i = 0; i < playersForThisGame.Count; i++)
            {
                EGS_PlayerData playerData = new EGS_PlayerData(i, playersForThisGame[i].GetUser().GetUsername());

                updateData.GetPlayersAtGame().Add(playerData);
            }

            // Message for the players.
            EGS_Message msg = new EGS_Message();
            msg.messageType = "GAME_FOUND";
            msg.messageContent = JsonUtility.ToJson(updateData);

            string jsonMSG = msg.ConvertMessage();

            // Message the players so they know that found a game.
            foreach (EGS_Player p in playersForThisGame)
            {
                Send(p.GetUser().GetSocket(), jsonMSG);
            }
        }
    }

    private void HeartbeatClient(Socket client_socket)
    {
        socketTimeoutCounters.Add(client_socket, new System.Timers.Timer(3000));
        socketTimeoutCounters[client_socket].Start();
        socketTimeoutCounters[client_socket].Elapsed += (sender, e) => DisconnectClientByTimeout(sender, e, client_socket);
    }

    private void DisconnectClientByTimeout(object sender, ElapsedEventArgs e, Socket client_socket)
    {
        DisconnectClient(client_socket);
    }

    public void DisconnectClient(Socket client_socket)
    {
        onDisconnectDelegate(client_socket);
        socketTimeoutCounters[client_socket].Close();
        socketTimeoutCounters.Remove(client_socket);

        //EGS_GamesManager.gm_instance.FinishGame(room);
    }

    private void TestMessage(Socket client_socket)
    {
        // Send the test message
        EGS_Message msg = new EGS_Message();
        msg.messageType = "TEST_MESSAGE";
        msg.messageContent = "";
        string jsonMSG = msg.ConvertMessage();

        Send(client_socket, jsonMSG);
    }
    #endregion
}