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
|   CompilerCore.cs
+---Example
|       Example.csproj
|       Example.cs
|       ScriptBase.cs - also gets compiled in even though not necessary TODOX better way to handle?
|       Game999.csx
|       Utils.csx
\---lib
        Ephemera.NBagOfTricks.dll


# TODOX slow startup 
Probably loading all the Roslyn stuff takes a while. Nothing to be done for Example (run to completion then exit)
but long-running should have a background warmup utility.


# TODOX Reflection or alternative

This uses basic method.Invoke(...) to access the members of the loaded script assembly.

Alternatives:

- method.CreateDelegate<T>. Should get 10X at least.
  https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
  Note .NET 7 introduced a behind the scenes delegate generation and caching which should improve this significantly,
  except for the first invocation of course. Not tested.
  https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/#reflection

- dynamic:
  https://stackoverflow.com/a/7189780

- UnsafeAccessorAttribute
  https://steven-giesel.com/blogPost/05ecdd16-8dc4-490f-b1cf-780c994346a4

# TODOX does this apply?

https://carljohansen.wordpress.com/2020/05/09/compiling-expression-trees-with-roslyn-without-memory-leaks-2/

Not for the Example (run to completion then exit) but yes for long-running apps that reload externally modified scripts.

see westwind:
Assemblies, once loaded, cannot be unloaded until the process shuts down or the AssemblyLoadContext is unloaded. In  .NET Framework there's no way to unload, but in .NET Core you can use an alternate AssemblyLoadContext.
In .NET Core it's possible to unload assemblies using the `CSharpScriptExecution.AlternateAssemblyLoadContext` which if provided can be used to unload assemblies loaded in the context conditionally.


