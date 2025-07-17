
// An included file with some app utility functions.

int notavar = 911;

public bool Boing(int which = 0)
{
    bool boinged = true;

    if (which == 0)
    {
        which = 555;
        boinged = true;
    }

    return boinged;
}
