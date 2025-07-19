# NScript
A script engine for embedding in .NET apps.

Compiles C#-like scripts into in-memory assemblies using Roslyn.

The Example directory demonstrates the use. There are two forms:

- Using simple reflection with direct Invoke() and delegates.
- Direct invocation of a late-bound base class.

Note it's also possible to do this with an interface.

Used by [Nebulator](https://github.com/cepthomas/Nebulator/blob/main/README.md)
and [NProcessing](https://github.com/cepthomas/NProcessing/blob/main/README.md).

Requires VS2022 and .NET8.


# The Files

```
NScript
|   NScript.sln/csproj - creates the NScript library
|   CompilerCore.cs - does most of the Roslyn work
|   Common.cs - mainly used by CompilerCore.cs
|
+---Example
    |   Example.csproj - builds the example app using NScript library
    |   Example.cs - compile-time and run-time parts of the example app
|   |   Dev.cs - development play area
|   |   Program.cs - Main()
    |
    \---Script
            Script.csproj - creates the base assembly
            ScriptCore.cs - run-time services and utilities for the script
            Game1.csx - script for reflection style
            Game2.csx - script for bound style
            Utils.csx - included by above

```


# Tech Notes

For long-running applications (not the examples), recompiling and reloading a script results in multiple
copies in the executing assembly. To prevent this, the previous assembly must be unloaded first.
This requires jumping through some hoops that can be problematic. Feeble unsuccessful attempts are described in Dev.cs.
Since the script dlls are typically tiny, we're not going to worry about a proliferation in memory right now.

This uses some custom preprocesser directives. Tools can add new tokens following the `#:`` convention:
https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives

This can have a slow startup, probably due to loading all the Roslyn stuff. Nothing to be done for run-to-completion
applications like Example. Long-running flavors will take the hit on the first invocation but should be speedy after that.
Unfortunately it gets a lot worse when running in VS.
