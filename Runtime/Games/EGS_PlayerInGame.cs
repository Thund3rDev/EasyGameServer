using UnityEngine;

public class EGS_PlayerInGame : MonoBehaviour
{
    private EGS_Player player;
    private int ingameID;
    private Vector3 movement;

    private void Start()
    {
        ingameID = int.Parse(this.name[this.name.Length - 1].ToString());
    }

    public void UpdatePosition(Vector3 playerPos)
    {
        movement = playerPos - this.transform.position;
        this.transform.position = new Vector3(playerPos.x, playerPos.y, playerPos.z);
    }

    /// <summary>
    /// Getter for Player.
    /// </summary>
    /// <returns>Player</returns>
    public EGS_Player GetPlayer() { return player; }

    /// <summary>
    /// Setter for Player.
    /// </summary>
    /// <param name="p">New Player</param>
    public void SetPlayer(EGS_Player p) { player = p; }

    /// <summary>
    /// Getter for the ingame ID.
    /// </summary>
    /// <returns>Ingame ID</returns>
    public int GetIngameID() { return ingameID; }

    /// <summary>
    /// Setter for the ingame ID.
    /// </summary>
    /// <param name="i">New ingame ID</param>
    public void SetIngameID(int i) { ingameID = i; }
}
