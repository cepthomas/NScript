
// Preprocesser directives

#:include Utils.csx
#:kustom cheeseburger with onions
#:directive_with_no_arg // for a test

Utils u = new();

JUNK // for a test

// Required overrides.
public override int Setup(string info, int worldX, int worldY)
{
    Print($"Setup(): {info}");
    _worldX = worldX;
    _worldY = worldY;

    CreatePlayer(Role.Oligarch, "Vladimir");
    CreatePlayer(Role.Sycophant, "Donald");
    CreatePlayer(Role.Hero, "Volodymyr ");
    CreatePlayer(Role.Peon, "Bob");
    CreatePlayer(Role.Peon, "Bob Also");
    CreatePlayer(Role.Peon, "Yet Another Bob");

    bool b = u.Boing(0);
    Print($"Boinged {b}");

    return 0;
}

public override int Move()
{
    Print($"Move()");

    RealTime += 5;

    var player = RandomPlayer();

    if (player == "")
    {
        Print($"No players left");
    }
    else if (_players.ContainsKey(player))
    {
        Print($"Player killed: {player}:{_players[player]}");
        _players.Remove(player);
    }
    else
    {
        Print($"This should never happen: {player}");
    }

    return 0;
}
