
// Preprocesser directives
#:include Utils.csx
#:kustom cheeseburger
#:dir_no_arg


public class Game999 : GameScriptBase
{
    Utils u = new();

    JUNK

    // Required overrides.
    public override int Setup(string info)
    {
        Print($"Setup(): {info}");

        WorldX = 100;
        WorldY = 50;

        CreatePlayer(Role.Oligarch, "Vladimir");
        CreatePlayer(Role.Sycophant, "Donald");
        CreatePlayer(Role.Hero, "Volodymyr ");
        CreatePlayer(Role.Peon, "Bob");
        CreatePlayer(Role.Peon, "Bob Also");
        CreatePlayer(Role.Peon, "Yet Another Bob");

        bool b = u.Boing(0); // 60

        return 0;
    }    

    public override int Move()
    {
        Print($"Move()");

        var player = RandomPlayer();
        if (Players.ContainsKey(player))
        {
            Print($"Player killed: {player} {Players[player]}");
            Players.Remove(player);
        }
        else
        {
            Print($"Player already gone:{player}");
        }

        return 0;
    }
}
    