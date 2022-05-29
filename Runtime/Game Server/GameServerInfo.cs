using UnityEngine;
using TMPro;

/// <summary>
/// Class GameServerInfo that will show information about the Game Server and players connected.
/// </summary>
public class GameServerInfo : MonoBehaviour
{
    #region Variables
    [Header("GameServer Info")]
    [Tooltip ("Text indicating the Game Server ID")]
    [SerializeField]
    private TextMeshProUGUI gameServerID_text;

    [Tooltip("Text showing info about the players connected")]
    [SerializeField]
    private TextMeshProUGUI playersInfo_text;

    [Tooltip("Constant string that stores the format for the players info")]
    private const string playerTemplate = "- {player_name}: {ping} ms";
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Start, that is called before the first frame update.
    /// </summary>
    private void Start()
    {
        // Only execute on Game Server.
        if (!EasyGameServerControl.instance.instanceType.Equals(EasyGameServerControl.EnumInstanceType.GameServer))
        {
            Destroy(this.gameObject);
        }
        else
        {
            gameServerID_text.text = "Game Server: " + GameServer.instance.GetGameServerID();
        }
    }

    /// <summary>
    /// Method Update, that is called once per frame.
    /// </summary>
    private void Update()
    {
        // Construct the information text and update it.
        string newPlayersInfoText = "";
        foreach (NetworkPlayer player in GameServer.instance.GetGame().GetPlayers())
        {
            UserData thisUser = player.GetUser();
            string thisPlayerText = playerTemplate;
            thisPlayerText = thisPlayerText.Replace("{player_name}", thisUser.GetUsername());

            string playerPing = GameServer.instance.GetGameServerSocketsManager().GetServerSocketHandler().GetLastRTTFromUser(thisUser).ToString();
            thisPlayerText = thisPlayerText.Replace("{ping}", playerPing);

            newPlayersInfoText += thisPlayerText + "\n";
        }

        newPlayersInfoText.TrimEnd('\n');
        playersInfo_text.text = newPlayersInfoText;
    }
    #endregion
}
