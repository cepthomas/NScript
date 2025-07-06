# NScript
A script engine primarily for embedding in .NET apps.

Compiles C#-like scripts into in-memory assemblies. Primarily for use by [Nebulator](https://github.com/cepthomas/Nebulator/blob/main/README.md)
and [NProcessing](https://github.com/cepthomas/NProcessing/blob/main/README.md). See those repos for example on how to use this.

Requires VS2022 and .NET8.

No dependencies on external components.


Tools can add new tokens following the #: convention.
https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives



https://www.michalkomorowski.com/2016/10/roslyn-how-to-create-custom-debuggable_27.html


// TODO1 improve on this?
//https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/#reflection
// Supposedly delegate is created under the cover so we don't have to write it.
//If you know at compile - time the signature of the target method use MethodInfo.CreateDelegate
//https://learn.microsoft.com/dotnet/api/system.reflection.methodinfo.createdelegate

var methodInit = scriptType.GetMethod("Init");
var methodSetup = scriptType.GetMethod("Setup");
var methodMove = scriptType.GetMethod("Move");

methodInit.Invoke(script, [Console.Out]);
methodSetup.Invoke(script, ["Here I am!!!", "too many"]);

