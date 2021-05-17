using UnityEngine;

/// <summary>
/// Class EGS_PlayerData, that contains the information of a player to send.
/// </summary>
[System.Serializable]
public class EGS_PlayerData
{
    #region Variables
    [SerializeField]
    private string username;

    [SerializeField]
    private Vector3 position;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_PlayerData()
    {
        this.username = "";
        this.position = new Vector3();
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the user name.
    /// </summary>
    /// <returns>User name</returns>
    public string GetUsername() { return username; }

    /// <summary>
    /// Setter for the user name.
    /// </summary>
    /// <param name="u">New User name</param>
    public void SetUsername(string u) { username = u; }

    /// <summary>
    /// Getter for the position.
    /// </summary>
    /// <returns>Position</returns>
    public Vector3 GetPosition() { return position; }

    /// <summary>
    /// Setter for the position.
    /// </summary>
    /// <param name="p">New position</param>
    public void SetPosition(Vector3 p) { position = p; }
    #endregion

}
