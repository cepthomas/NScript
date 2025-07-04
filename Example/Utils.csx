
// An included file with app utility functions.
public class Utils : GameScriptBase
{
    int notavar = 911;

    public bool Boing(int which = 0)
    {
        bool boinged = true;

        if (which == 0)
        {
            which = 555;
            boinged = true;
        }

        Print($"Boinged {RandomPlayer()}");

        return boinged;
    }
}