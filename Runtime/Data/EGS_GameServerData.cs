using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public class EGS_GameServerData
{
    #region Variables
    // Enum to define game server states.
    public enum State
    {
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
    #endregion

    #region Constructors
    public EGS_GameServerData()
    {

    }

    public EGS_GameServerData(int gameServer_ID_)
    {
        gameServer_ID = gameServer_ID_;
        status = State.LAUNCH;
    }
    #endregion
}
