/// <summary>
/// Static Class GameServerMessageTypes, containing the PREDEFINED type of messages that GameServers can receive.
/// </summary>
public static class GameServerMessageTypes
{
    public const string
        // As Server.
        RTT_RESPONSE_CLIENT = "RTT_RESPONSE_CLIENT",
        JOIN_GAME_SERVER = "JOIN_GAME_SERVER",
        PLAYER_INPUT = "PLAYER_INPUT",
        LEAVE_GAME = "LEAVE_GAME",
        RETURN_TO_MASTER_SERVER = "RETURN_TO_MASTER_SERVER",

        // As Client.
        RTT = "RTT",
        CONNECT_TO_MASTER_SERVER = "CONNECT_TO_MASTER_SERVER",
        RECEIVE_GAME_DATA = "RECEIVE_GAME_DATA",
        DISCONNECT_AND_CLOSE_GAMESERVER = "DISCONNECT_AND_CLOSE_GAMESERVER",
        MASTER_SERVER_CLOSE_GAME_SERVER = "MASTER_SERVER_CLOSE_GAME_SERVER";
}
