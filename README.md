# NScript
A script engine primarily for embedding in .NET apps.

Compiles C#-like scripts into in-memory assemblies. Primarily for use by [Nebulator](https://github.com/cepthomas/Nebulator/blob/main/README.md)
and [NProcessing](https://github.com/cepthomas/NProcessing/blob/main/README.md). See those repos for example on how to use this.

Requires VS2022 and .NET8.

No dependencies on external components.



C:\Dev\Libs\NScript
|   NScript.sln
|   NScript.csproj
|   Common.cs
|   Engine.cs
+---Example
|       Example.csproj
|       Example.cs
|       ScriptBase.cs
|       Game999.csx
|       Utils.csx
\---lib
        Ephemera.NBagOfTricks.dll


