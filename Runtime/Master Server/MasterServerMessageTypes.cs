/// <summary>
/// Static Class MasterServerMessageTypes, containing the PREDEFINED type of messages that MasterServer can receive.
/// </summary>
public static class MasterServerMessageTypes
{
    public const string
        RTT_RESPONSE_CLIENT = "RTT_RESPONSE_CLIENT",
        RTT_RESPONSE_GAME_SERVER = "RTT_RESPONSE_GAME_SERVER",
        USER_JOIN_SERVER = "USER_JOIN_SERVER",
        DISCONNECT_USER = "DISCONNECT_USER",
        QUEUE_JOIN = "QUEUE_JOIN",
        QUEUE_LEAVE = "QUEUE_LEAVE",
        DISCONNECT_TO_GAME = "DISCONNECT_TO_GAME",
        USER_LEAVE_GAME = "USER_LEAVE_GAME",
        CREATED_GAME_SERVER = "CREATED_GAME_SERVER",
        READY_GAME_SERVER = "READY_GAME_SERVER",
        GAME_START = "GAME_START",
        GAME_END = "GAME_END",
        USER_DELETE = "USER_DELETE";
}
