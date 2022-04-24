using UnityEngine;
using TMPro;

public class EGS_GameServerEndController : MonoBehaviour
{
    #region Variables
    [Header("General Variables")]
    [Tooltip("Singleton")]
    public static EGS_GameServerEndController instance;

    private int numPlayers;
    private int numPlayersStillConnected;
    private bool disconnectedFromMasterServer;

    // TEST .
    // TODO: Make a Game Server Console with Log and UI.
    public TextMeshProUGUI test_text;
    public GameObject endGameInfoGameobject;

    #endregion
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        disconnectedFromMasterServer = false;
        numPlayers = EGS_GameServer.instance.gameServerSocketsController.GetPlayersConnected();
        numPlayersStillConnected = numPlayers;
    }

    public void ShowEndGameInfo()
    {
        endGameInfoGameobject.SetActive(true);
        test_text.text += "\n Players remaining to disconnect: " + numPlayersStillConnected + " / " + numPlayers;
    }

    public void UpdateNumOfPlayersConnected(EGS_GS_Sockets socketsController)
    {
        numPlayersStillConnected--;

        if (numPlayersStillConnected == 0)
            socketsController.StopListening();

        CheckToCloseGameServer();

        test_text.text += "\n Players remaining to disconnect: " + numPlayersStillConnected + " / " + numPlayers;
    }

    public void UpdateDisconnectedFromMasterServer()
    {
        disconnectedFromMasterServer = true;
        CheckToCloseGameServer();

        test_text.text += "\n Disconnected from the Master Server.";
    }

    private void CheckToCloseGameServer()
    {
        if (numPlayersStillConnected == 0 && disconnectedFromMasterServer)
            CloseGameServer();
    }

    public void CloseGameServer()
    {
        // Call the onGameServerShutdown delegate.
        EGS_GameServerDelegates.onGameServerShutdown?.Invoke();

        Application.Quit();
    }
}
