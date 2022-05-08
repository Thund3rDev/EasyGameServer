using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class EGS_GameManager, that manages the game and links the data between Unity and the Game.
/// </summary>
public class EGS_GameManager : MonoBehaviour
{
    #region Variables
    [Header("General")]
    [Tooltip("Singleton instance")]
    public static EGS_GameManager instance = null;


    [Header("Players")]
    [Tooltip("List of player instances in the game")]
    [SerializeField] private List<EGS_Player> playersInGame = new List<EGS_Player>();

    [Tooltip("List of players by their ID")]
    private Dictionary<int, EGS_Player> playersByID = new Dictionary<int, EGS_Player>();


    [Header("References")]
    [Tooltip("Client Game Manager Script to destroy")]
    [SerializeField] private MonoBehaviour gameManager = null; // TODO: List of scripts to delete.
    #endregion

    #region Unity Methods
    /// <summary>
    /// Method Awake, executed on script load.
    /// </summary>
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

    /// <summary>
    /// Method Start, executed before the first frame.
    /// </summary>
    private void Start()
    {
        // If not executing on the game server, delete this script.
        if (!EGS_Control.instance.egs_type.Equals(EGS_Control.EGS_Type.GameServer))
        {
            Destroy(this.gameObject);
        }
        else
        {
            // Destroy the client game manager GameObject. // TODO: Not needed, client game manager should check with EGS_Type.
            Destroy(gameManager.gameObject);

            // For each player in the game.
            foreach (EGS_Player player in playersInGame)
            {
                // Assign User info.
                foreach (EGS_User userToGame in EGS_GameServer.instance.gameFoundData.GetUsersToGame())
                {
                    if (player.GetIngameID() == userToGame.GetIngameID())
                    {
                        player.SetUser(userToGame);
                    }
                }

                // Add player to lists.
                EGS_GameServer.instance.thisGame.AddPlayer(player);
                playersByID.Add(player.GetIngameID(), player);
            }

            // Get the start game positions.
            EGS_UpdateData startUpdateData = new EGS_UpdateData(EGS_GameServer.instance.thisGame.GetRoom());

            foreach (EGS_Player player in EGS_GameServer.instance.thisGame.GetPlayers())
            {
                EGS_PlayerData playerData = new EGS_PlayerData(player.GetIngameID(), player.transform.position, new Vector3());
                startUpdateData.GetPlayersAtGame().Add(playerData);
            }

            // Create the message to send to the players and master server.
            string gameStartMessageContent = JsonUtility.ToJson(startUpdateData);
            EGS_Message messageToSend = new EGS_Message("GAME_START", gameStartMessageContent);

            // Log the players connected on GameStart.
            string playersString = "";
            foreach (EGS_User user in EGS_GameServer.instance.gameFoundData.GetUsersToGame())
            {
                playersString += user.GetUsername() + ", ";
            }

            EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\n" + EGS_GameServer.instance.thisGame.GetPlayers().Count + " | " + playersString; });

            // For each user / player, send the game start message.
            foreach (EGS_User user in EGS_GameServer.instance.gameFoundData.GetUsersToGame())
            {
                EGS_Dispatcher.RunOnMainThread(() => { EGS_GameServer.instance.test_text.text += "\nSEND TO : " + user.GetUsername(); });
                EGS_GameServer.instance.SendMessageToClient(user.GetSocket(), messageToSend);
            }

            // TODO: Send to the master server the info of the started game.

            // Call the onGameStart delegate.
            EGS_GameServerDelegates.onGameStart?.Invoke();
        }
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for PlayersInGame.
    /// </summary>
    /// <returns>List of players in Game.</returns>
    public List<EGS_Player> GetPlayersInGame() { return playersInGame; }

    /// <summary>
    /// Setter for PlayersInGame.
    /// </summary>
    /// <param name="p">New List of players in Game.</param>
    public void SetPlayersInGame(List<EGS_Player> p) { playersInGame = p; }

    /// <summary>
    /// Getter for PlayersByID.
    /// </summary>
    /// <returns>List of players by ID.</returns>
    public Dictionary<int, EGS_Player> GetPlayersByID() { return playersByID; }

    /// <summary>
    /// Setter for PlayersByID.
    /// </summary>
    /// <param name="p">New List of players by ID.</param>
    public void SetPlayersByID(Dictionary<int, EGS_Player> p) { playersByID = p; }
    #endregion
}
