
// Preprocesser directives  TODO1 also . and .. in:
#:include .\Utils.csx // ok?
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
    RealTime += 5;

    var player = RandomPlayer();

    if (player is null)
    {
        Print($"No players left");
    }
    else
    {
        var role = GetRole(player);
        Print($"Player killed: {player}. Alas, they were a good {role}");
        Remove(player);
    }

    return 0;
}
