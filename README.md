# NScript  TODOX
A script engine primarily for embedding in .NET apps.

Compiles C#-like scripts into in-memory assemblies. Primarily for use by [Nebulator](https://github.com/cepthomas/Nebulator/blob/main/README.md)
and [NProcessing](https://github.com/cepthomas/NProcessing/blob/main/README.md). See those repos for example on how to use this.

Requires VS2022 and .NET8.

No dependencies on external components.

// Preprocesser directives
// Tools can add new tokens following the #: convention.
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives


C:\Dev\Libs\NScript
|   NScript.sln
|   NScript.csproj
|   Common.cs
|   Engine.cs
+---Example
|       Example.csproj
|       Example.cs
|       ScriptBase.cs - also gets compiled in even though not necessary TODOX better way to handle?
|       Game999.csx
|       Utils.csx
\---lib
        Ephemera.NBagOfTricks.dll


