
// Preprocesser directives
#:include Utils.csx
#:kustom french fries

Utils u = new();

JUNK // for a test

// Required overrides.
public override int Setup(string info, int worldX, int worldY)
{
    Print($"Setup(): {info} size:{worldX}x{worldY}");
    _worldX = worldX;
    _worldY = worldY;

    CreatePlayer(Role.Oligarch, "Darth");
    CreatePlayer(Role.Sycophant, "Erv");
    CreatePlayer(Role.Hero, "Han");
    CreatePlayer(Role.Hero, "Leia");
    CreatePlayer(Role.Hero, "Luke");
    CreatePlayer(Role.Peon, "Chewbacca");
    CreatePlayer(Role.Peon, "R2-D2");
    CreatePlayer(Role.Peon, "C-3PO");

    bool b = u.Boing(0);
    Print($"Boinged {b}");

    return 0;
}

public override int Move()
{
    Print($"Move()");

    RealTime += 6.1;

    var player = RandomPlayer();

    if (player is null)
    {
        Print($"No players left");
    }
    else
    {
        Print($"Player killed: {player}:{_players[player]}");
        _players.Remove(player);
    }

    return 0;
}
