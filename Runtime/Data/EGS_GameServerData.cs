using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class EGS_GameServerData
{
    #region Variables
    // Enum to define game server states.
    public enum State
    {
        INACTIVE,
        LAUNCH,
        CREATED,
        WAITING_PLAYERS,
        STARTED,
        FINISHED
    }

    private Process process;
    public Process Process { get => process; set => process = value; }

    private int gameServer_ID;
    public int GameServer_ID { get => gameServer_ID; set => gameServer_ID = value; }

    private State status;
    public State Status { get => status; set => status = value; }

    private int room_ID;
    public int Room_ID { get => room_ID; set => room_ID = value; }
    #endregion

    #region Constructors
    public EGS_GameServerData()
    {

    }

    public EGS_GameServerData(int gameServer_ID_, int room_ID_)
    {
        gameServer_ID = gameServer_ID_;
        room_ID = room_ID_;
        status = State.LAUNCH;
    }
    #endregion
}
