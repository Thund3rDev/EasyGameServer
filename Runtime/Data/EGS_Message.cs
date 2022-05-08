using UnityEngine;
/// <summary>
/// Class EGS_Message, to send messages on sockets.
/// </summary>
[System.Serializable]
public class EGS_Message
{
    #region Variables
    [Header("Message")]
    [Tooltip("Type of the message")]
    [SerializeField]
    private string messageType;

    [Tooltip("Content of the message")]
    [SerializeField]
    private string messageContent;
    #endregion

    #region Constructors
    /// <summary>
    /// Empty Constructor.
    /// </summary>
    public EGS_Message() {}

    /// <summary>
    /// Type only Constructor.
    /// </summary>
    /// <param name="messageType">Type of the message</param>
    public EGS_Message(string messageType)
    {
        this.messageType = messageType;
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
    /// <summary>
    /// Method ConvertMessage, that makes the json serialization and adds the "End of Message" code.
    /// </summary>
    /// <returns></returns>
    public string ConvertMessage()
    {
        return JsonUtility.ToJson(this) + "<EOM>";
    }
    #endregion

    #region Getters and Setters
    /// <summary>
    /// Getter for the Message Type.
    /// </summary>
    /// <returns>Type of the message</returns>
    public string GetMessageType() { return messageType; }

    /// <summary>
    /// Setter for the Message Type.
    /// </summary>
    /// <param name="messageType">New type of the message</param>
    public void SetMessageType(string messageType) { this.messageType = messageType; }

    /// <summary>
    /// Getter for the Message Content.
    /// </summary>
    /// <returns>Content of the message</returns>
    public string GetMessageContent() { return messageContent; }

    /// <summary>
    /// Setter for the Message Content.
    /// </summary>
    /// <param name="messageContent">New content of the message</param>
    public void SetMessageContent(string messageContent) { this.messageContent = messageContent; }

    #endregion
}
