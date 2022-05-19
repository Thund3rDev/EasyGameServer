using UnityEngine;
using TMPro;
using System.Threading;
using System;

/// <summary>
/// Class GameServerEndController, that will check if Game Server can end and be closed.
/// </summary>
public class GameServerEndController : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static GameServerEndController instance;


    [Header("End Game Server Control")]
    [Tooltip("Number of players on the Game Server")]
    private int numPlayers;

    [Tooltip("Number of players still connected to the Game Server")]
    private int numPlayersStillConnected;

    [Tooltip("Bool that indicates if Game Server is disconnected from the master server")]
    private bool disconnectedFromMasterServer;

    [Tooltip("Mutex to control the number of players still connected and the checks with it")]
    private Mutex numPlayers_mutex = new Mutex();


    // TODO: Make a Game Server Console with Log and UI.
    [Header("End Game Server UI")]
    [Tooltip("Text where the end game server info will be written")]
    [SerializeField]
    private TextMeshProUGUI endGameServer_text;

    [Tooltip("GameObject which contains the End Game Server Info")]
    [SerializeField]
    private GameObject endGameInfoGameobject;
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, executed on script load.
    /// </summary>
    private void Awake()
    {
        // Instantiate the singleton.
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// Method Start, that is called before the first frame update.
    /// </summary>
    private void Start()
    {
        // Reset and get the values.
        this.disconnectedFromMasterServer = false;
        this.numPlayers = GameServer.instance.GetGameServerSocketsController().GetPlayersConnected();
        this.numPlayersStillConnected = numPlayers;
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method ShowEndGameInfo, that will show the End Game Info GameObject and will update the text with the current data.
    /// </summary>
    public void ShowEndGameInfo()
    {
        this.endGameInfoGameobject.SetActive(true);
        this.endGameServer_text.text += "\n Players remaining to disconnect: " + numPlayersStillConnected + " / " + numPlayers;
    }

    /// <summary>
    /// Method UpdateNumOfPlayersConnected, called when another client leaves the game server.
    /// </summary>
    /// <param name="socketsController"></param>
    public void UpdateNumOfPlayersConnected(GameServerSocketManager socketsController)
    {
        try
        {
            // Wait for the mutex.
            numPlayers_mutex.WaitOne();

            // Update the number of players still connected and if it is 0, stop listening.
            numPlayersStillConnected--;

            if (numPlayersStillConnected == 0)
                socketsController.StopListening();

            // Check if can close the Game Server.
            CheckToCloseGameServer();

            // Update the end game server text.
            endGameServer_text.text += "\n Players remaining to disconnect: " + numPlayersStillConnected + " / " + numPlayers;
        }
        catch (Exception)
        {
            // LOG.
        }
        finally
        {
            // Release the Mutex.
            numPlayers_mutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// Method UpdateDisconnectedFromMasterServer, that will update the disconnectedFromMasterServer value and check if can close the Game Server.
    /// </summary>
    public void UpdateDisconnectedFromMasterServer()
    {
        // Update the disconnectedFromMasterServer value.
        disconnectedFromMasterServer = true;

        // Check if can close the Game Server.
        CheckToCloseGameServer();

        // Update the end game server text.
        endGameServer_text.text += "\n Disconnected from the Master Server.";
    }

    /// <summary>
    /// Method CheckToCloseGameServer, that will close the Game Server if can do it.
    /// </summary>
    private void CheckToCloseGameServer()
    {
        // If number of players still connected is 0 and Game Server is disconnected from the master server, close the Game Server.
        if (numPlayersStillConnected == 0 && disconnectedFromMasterServer)
            CloseGameServer();
    }

    /// <summary>
    /// Method CloseGameServer, that will close the Game Server.
    /// </summary>
    public void CloseGameServer()
    {
        // Call the onGameServerShutdown delegate.
        GameServerDelegates.onGameServerShutdown?.Invoke();

        // Close the Game Server.
        Application.Quit();
    }
    #endregion
}
