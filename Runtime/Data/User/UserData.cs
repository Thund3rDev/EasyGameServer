using UnityEngine;

/// <summary>
/// Class UserData, that contains the structure of an user in the server.
/// [MODIFIABLE].
/// </summary>
[System.Serializable]
public class UserData : BaseUserData
{
    #region Variables
    [Header("Game")]
    [Tooltip("Selected Character ID")]
    [SerializeField]
    protected int selectedCharacterID; // You can delete this! It was for the prototype.
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public UserData() : base()
    {
        this.selectedCharacterID = -1;
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the Selected Character ID.
    /// </summary>
    /// <returns>Selected Character ID</returns>
    public int GetSelectedCharacterID() { return selectedCharacterID; }

    /// <summary>
    /// Setter for the Selected Character ID.
    /// </summary>
    /// <param name="SetSelectedCharacterID">New Selected Character ID</param>
    public void SetSelectedCharacterID(int SetSelectedCharacterID) { this.selectedCharacterID = SetSelectedCharacterID; }
    #endregion
}