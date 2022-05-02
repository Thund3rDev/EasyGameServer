using UnityEngine;
/// <summary>
/// Class EGS_Message, to send messages on sockets.
/// </summary>
[System.Serializable]
public class EGS_Message
{
    #region Variables
    // Type of the message. Examples: login, user, connect, findGame...
    public string messageType;
    // Content of the message.
    public string messageContent;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public EGS_Message()
    {

    }

    /// <summary>
    /// Base constructor.
    /// </summary>
    /// <param name="messageType">Type of the message</param>
    /// <param name="messageContent">Content of the message</param>
    public EGS_Message(string messageType, string messageContent)
    {
        this.messageType = messageType;
        this.messageContent = messageContent;
    }
    #endregion

    #region Class Methods

    // Method ConvertMessage, that makes the json serialization and adds the "End of Message" code.
    public string ConvertMessage()
    {
        return JsonUtility.ToJson(this) + "<EOM>";
    }
    #endregion
}
