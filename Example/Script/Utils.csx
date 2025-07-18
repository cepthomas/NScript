
// An included file with some app utility functions.

int notavar = 911;

public bool Boing(int which = 0)
{
    bool boinged = which % 3 == 0;

    return boinged;
}
