# NScript
A script engine for embedding in .NET apps.

Compiles C#-like scripts into in-memory assemblies using Roslyn. Scripts do not use namespace or class
name declarations.

Used by [Nebulator](https://github.com/cepthomas/Nebulator/blob/main/README.md)
and [NProcessing](https://github.com/cepthomas/NProcessing/blob/main/README.md).

Requires VS2022 and .NET8.

No dependencies on third-party components (except for my libraries in `\lib`).


# The Files

```
NScript
|   NScript.sln/csproj - creates the NScript library
|   CompilerCore.cs - does most of the Roslyn work
|   Common.cs - mainly used by CompilerCore.cs
\---Example
        Example.csproj - builds the example app using NScript library
        Example.cs - compile-time and run-time parts of the example app
        ScriptCore.cs - run-time services and utilities for the script - *not* built by Example.csproj
        Game999.csx - top-level script - extension can be whatever you want
        Utils.csx - included by above
```


# Tech Notes

This uses some custom preprocesser directives. Tools can add new tokens following the `#:`` convention:
https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives

This can have a slow startup, probably due to loading all the Roslyn stuff. Nothing to be done for run-to-completion
applications like Example. Long-running flavors will take the hit on the first invocation but should be speedy after that.
Unfortunately it gets a lot worse when running in VS.

Uses .NET reflection to locate entry in the in-memory loaded assembly. In the Example delegates are used but
plain old method.Invoke(...) works too - depends on your application and preference.
