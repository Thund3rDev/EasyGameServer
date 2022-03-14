using UnityEngine;

/// <summary>
/// Class EGS_User, that contains the structure of an user in the server. MODIFIABLE.
/// </summary>
[System.Serializable]
public class EGS_User : EGS_User_Base
{
    #region Variables
    [Tooltip("Selected Character ID")]
    [SerializeField] protected int selectedCharacterID;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor
    /// </summary>
    public EGS_User() : base()
    {
        selectedCharacterID = -1;
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
    /// <param name="u">New Selected Character ID</param>
    public void SetSelectedCharacterID(int s) { selectedCharacterID = s; }
    #endregion
}
