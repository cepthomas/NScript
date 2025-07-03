
// Directives/preprocess: Include config and lib files.
Include("Utils.csx");

KustomDirective("cheeseburger")

public class Game999 : GameScriptApi
{
    Utils u = new();

    public void Nada()
    {

    }

    // Required overrides.
    public override int Setup()
    {
        Print($"Setup()");

        worldX = 100;
        worldY = 50;

        CreatePlayer(Role.Oligarch, "Vladimir");
        CreatePlayer(Role.Sycophant, "Donald");
        CreatePlayer(Role.Hero, "Volodymyr ");
        CreatePlayer(Role.Peon, "Bob");
        CreatePlayer(Role.Peon, "Bob Also");
        CreatePlayer(Role.Peon, "Yet Another Bob");

        bool b = u.Boing(0);
        //bool b = u.Boing(60);

        return 0;
    }    

    public override int Move()
    {
        Print($"Move()");

        var player = RandomPlayer();
        if (players.ContainsKey(player))
        {
            Print($"Player remove:{player}");
            players.Remove(player);
        }
        else
        {
            Print($"Player already gone:{player}");
        }

        return 0;
    }
}
    