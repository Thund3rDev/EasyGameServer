using System.Threading;

/// <summary>
/// Class EGS_GameControlData, that contains the data to control a game.
/// </summary>
public class EGS_GameControlData
{
    #region Variables
    private EGS_Game game;
    public EGS_Game Game { get => game; set => game = value; }

    private int startGame_Counter;
    public int StartGame_Counter { get => startGame_Counter; set => startGame_Counter = value; }

    private Mutex startGame_Lock;
    public Mutex StartGame_Lock { get => startGame_Lock; set => startGame_Lock = value; }
    #endregion

    #region Constructors
    public EGS_GameControlData()
    {

    }

    public EGS_GameControlData(EGS_Game game_)
    {
        Game = game_;
        startGame_Counter = 0;
        startGame_Lock = new Mutex();
    }
    #endregion
}
