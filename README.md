# NScript
A script engine primarily for embedding in .NET apps.

Compiles C#-like scripts into in-memory assemblies. Primarily for use by [Nebulator](https://github.com/cepthomas/Nebulator/blob/main/README.md)
and [NProcessing](https://github.com/cepthomas/NProcessing/blob/main/README.md). See those repos for example on how to use this.

Requires VS2022 and .NET8.

No dependencies on external components.


NScript
|   .gitignore
|   CompilerCore.cs
|   LICENSE
|   notes.txt
|   NScript.csproj
|   NScript.sln
|   README.md
|   
+---lib
|       Ephemera.NBagOfTricks.dll
|       Ephemera.NBagOfTricks.xml
|       
\---Test
        MyCompiler.cs
        Program.cs
        ScriptBase.cs ==> must be named this! TODO1
        Test.csproj
        Test_COMP.cs
        Utils.sctest
        Variation999.sctest
        
    // ScriptBase.cs - All the stuff to do app specific work
    // Define script type api - as virtual members
    // Store script data
    // Do runtime work/processing.


