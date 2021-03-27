using UnityEngine;
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

    // Method ConvertMessage, that makes the json serialization and adds the "End of Message" code.
    public string ConvertMessage()
    {
        string jsonMSG = JsonUtility.ToJson(this) + "<EOM>";
        return jsonMSG;
    }
}
