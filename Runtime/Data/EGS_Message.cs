/// <summary>
/// Class EGS_Message, to send messages on sockets.
/// </summary>
[System.Serializable]
public class EGS_Message
{
    // Type of the message. Examples: login, user, connect, findGame...
    public string messageType;
    // Content of the message.
    public string messageContent;
}
