/// <summary>
/// Static Class ClientMessageTypes, containing the PREDEFINED type of messages that Clients can receive.
/// </summary>
public static class ClientMessageTypes
{
    public const string
        RTT = "RTT",
        CONNECT_TO_MASTER_SERVER = "CONNECT_TO_MASTER_SERVER",
        JOIN_MASTER_SERVER = "JOIN_MASTER_SERVER",
        DISCONNECT = "DISCONNECT",
        GAME_FOUND = "GAME_FOUND",
        CHANGE_TO_GAME_SERVER = "CHANGE_TO_GAME_SERVER",
        DISCONNECT_TO_GAME = "DISCONNECT_TO_GAME",
        CONNECT_TO_GAME_SERVER = "CONNECT_TO_GAME_SERVER",
        JOIN_GAME_SERVER = "JOIN_GAME_SERVER",
        GAME_START = "GAME_START",
        UPDATE = "UPDATE",
        PLAYER_LEAVE_GAME = "PLAYER_LEAVE_GAME",
        GAME_END = "GAME_END",
        DISCONNECT_TO_MASTER_SERVER = "DISCONNECT_TO_MASTER_SERVER",
        RETURN_TO_MASTER_SERVER = "RETURN_TO_MASTER_SERVER",
        CLOSE_SERVER = "CLOSE_SERVER",
        USER_DELETE = "USER_DELETE";
}
