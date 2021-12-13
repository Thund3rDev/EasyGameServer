using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EGS_GameManager : MonoBehaviour
{
    private List<EGS_PlayerInGame> playersInGame = new List<EGS_PlayerInGame>();

    // Start is called before the first frame update
    void Start()
    {
        playersInGame = FindObjectsOfType<EGS_PlayerInGame>().ToList();
        LinkPlayers();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (EGS_PlayerInGame playerInGame in playersInGame)
        {
            playerInGame.UpdatePosition(playerInGame.GetPlayer().GetPosition());
        }
    }

    private void LinkPlayers()
    {
        foreach (EGS_Player player in EGS_GameServer.gameServer_instance.thisGame.GetPlayers())
        {
            foreach (EGS_PlayerInGame playerInGame in playersInGame)
            {
                if (player.GetIngameID() == playerInGame.GetIngameID())
                {
                    playerInGame.SetPlayer(player);
                    break;
                }
            }
        }
    }
}
