using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class NetworkGameManager, that manages the game and links the data between Unity and the Game.
/// </summary>
public class NetworkGameManager : MonoBehaviour
{
    #region Variables
    [Header("General")]
    [Tooltip("Singleton instance")]
    public static NetworkGameManager instance = null;


    [Header("Players")]
    [Tooltip("List of player instances in the game")]
    [SerializeField]
    private List<NetworkPlayer> playersInGame = new List<NetworkPlayer>();


    [Header("References")]
    [Tooltip("List of Client Scripts to DELETE")]
    [SerializeField]
    private List<MonoBehaviour> clientScriptsToDelete = null;
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
    /// Method Start, executed before the first frame.
    /// </summary>
    private void Start()
    {
        // If not executing on the game server, delete this script.
        if (!EasyGameServerControl.instance.instanceType.Equals(EasyGameServerControl.EnumInstanceType.GameServer))
        {
            Destroy(this.gameObject);
        }
        else
        {
            // Delete the scripts on the clientScriptsToDelete list.
            foreach (MonoBehaviour script in clientScriptsToDelete)
                Destroy(script);

            // For each player in the game.
            foreach (NetworkPlayer player in playersInGame)
            {
                // Assign User info.
                foreach (UserData userToGame in GameServer.instance.GetGameFoundData().GetUsersToGame())
                {
                    if (player.GetIngameID() == userToGame.GetIngameID())
                    {
                        player.SetUser(userToGame);
                    }
                }

                // Add player to lists.
                GameServer.instance.GetGame().AddPlayer(player);
            }

            // Get the start game positions.
            UpdateData startUpdateData = new UpdateData(GameServer.instance.GetGame().GetRoom());

            foreach (NetworkPlayer player in GameServer.instance.GetGame().GetPlayers())
            {
                PlayerData playerData = new PlayerData(player.GetIngameID(), player.transform.position, new Vector3());
                startUpdateData.GetPlayersAtGame().Add(playerData);
            }

            // Create the message to send to the players and master server.
            string gameStartMessageContent = JsonUtility.ToJson(startUpdateData);
            NetworkMessage messageToSend = new NetworkMessage(ClientMessageTypes.GAME_START, gameStartMessageContent);

            // Log the players connected on GameStart.
            string playersString = "";
            foreach (UserData user in GameServer.instance.GetGameFoundData().GetUsersToGame())
            {
                playersString += user.GetUsername() + ", ";
            }

            // LOG ?
            MainThreadDispatcher.RunOnMainThread(() => { GameServer.instance.console_text.text += "\n" + GameServer.instance.GetGame().GetPlayers().Count + " | " + playersString; });

            // For each user / player, send the game start message.
            foreach (UserData user in GameServer.instance.GetGameFoundData().GetUsersToGame())
            {
                // LOG ?
                MainThreadDispatcher.RunOnMainThread(() => { GameServer.instance.console_text.text += "\nSEND TO : " + user.GetUsername(); });
                GameServer.instance.SendMessageToClient(user.GetSocket(), messageToSend);
            }

            // Send to the master server the info of the started game.
            GameServer.instance.SendMessageToMasterServer(messageToSend);

            // Call the onGameStart delegate.
            GameServerDelegates.onGameStart?.Invoke();
        }
    }
    #endregion

    #region Class Methods
    /// <summary>
    /// Method GetPlayerByID, that finds a player on the list by its ID.
    /// </summary>
    /// <param name="playerID">Player ID</param>
    /// <returns>NetworkPlayer with the given ID</returns>
    public NetworkPlayer GetPlayerByID(int playerID)
    {
        return this.playersInGame.Find(player => player.GetIngameID() == playerID);
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for PlayersInGame.
    /// </summary>
    /// <returns>List of players in Game.</returns>
    public List<NetworkPlayer> GetPlayersInGame() { return playersInGame; }

    /// <summary>
    /// Setter for PlayersInGame.
    /// </summary>
    /// <param name="p">New List of players in Game.</param>
    public void SetPlayersInGame(List<NetworkPlayer> p) { playersInGame = p; }
    #endregion
}
