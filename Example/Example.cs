using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;


// faster reflection:
// https://steven-giesel.com/blogPost/05ecdd16-8dc4-490f-b1cf-780c994346a4
// https://sergiopedri.medium.com/optimizing-reflection-with-dynamic-code-generation-6e15cef4b1a2


// namespace __ScriptExecution {
// public class __Executor { 
//     public async Task<string> GetJsonFromAlbumViewer(int id)
//     {

// Loading and Executing
// Once you've figured out which dependencies to add and how, the rest of the compilation process is pretty easy.
// The result is an assembly that gets written to a stream which you can use to load the assembly:
//   assembly = Assembly.Load(((MemoryStream)codeStream).ToArray());
// To load the compiled type you can then use Reflection:
//   // Instantiate
//   dynamic instance = assembly.CreateInstance("__ScriptExecution.__Executor");
//   The result will be an object reference, and the easiest way to use it is by using dynamic.
//   // Call
//   var json = await instance.GetJsonFromAlbumViewer(37);
// There are other ways you can use this type of course:
//     Reflection
//     Typed Interfaces that are shared between host app and compiled code

//Define the interfaces that outline the contract between the host and the plugins in a separate class library project.
//The host application can dynamically load assemblies containing the compiled code (plugins) at runtime using mechanisms
//  like Assembly.LoadFrom().
//Reflection can then be used to identify types within the loaded assembly that implement the shared interface(s).
//Instances of these plugin types can be created using Activator.CreateInstance(), allowing the host application to
//  interact with the plugin through the shared interface contract.




namespace Example
{
    class Example
    {
        /// <summary>This demonstrates how a host application loads and runs scripts.</summary>
        /// <returns>Exit code: 0=ok 1=compiler or syntax error 2=runtime error</returns>
        public int Run()
        {
            Program.tm.Snap("20");

            ///// Compile script with application options.
            GameEngine engine = new()
            {
                ScriptPath = ".", //MiscUtils.GetSourcePath(),
                IgnoreWarnings = true, //false,
                Namespace = "Example.Script" // same as ScriptBase.cs
            };

            var scriptFile = Path.Combine(engine.ScriptPath, "Game999.csx");
            var baseFile = Path.Combine(engine.ScriptPath, "ScriptBase.cs");

            // Namespace should be the same as ScriptBase.cs.
            engine.CompileScript(scriptFile, [baseFile]);

            if (engine.CompiledScript is null)
            {
                Console.WriteLine($"Compile failed:");
                engine.Reports.ForEach(rep => Console.WriteLine($"{rep}"));
                return 1;
            }

            Program.tm.Snap("30");

            ///// Execute script. Needs exception handling to protect from user runtime script errors.
            try
            {
                // Init script.
                var script = engine.CompiledScript;
                var scriptType = script.GetType();

                // This uses basic method.Invoke(...). The record suggests significant performance improvement
                // using method.CreateDelegate<T>. However it's not the easiest tech to use. .NET 7 introduced
                // a behind the scenes delegate generation and caching which should improve this significantly,
                // except for the first invocation of course. Not tested.
                // https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/#reflection

                // Cache methods.
                var methodInit = scriptType.GetMethod("Init");
                var methodSetup = scriptType.GetMethod("Setup");
                var methodMove = scriptType.GetMethod("Move");

                Program.tm.Snap("40");

                // Run the game.
                methodInit.Invoke(script, [Console.Out]);
                methodSetup.Invoke(script, ["Here I am!!!"]); //, "too many args"]);

                for (int i = 0; i < 10; i++)
                { 
                    methodMove.Invoke(script, []);  
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script runtime failed:");
                engine.Reports.Clear();
                engine.ProcessRuntimeException(ex);
                engine.Reports.ForEach(rep => Console.WriteLine($"{rep}"));
                return 2;
            }

            Program.tm.Snap("50");

            return 0;
        }
    }

    /// <summary>The accompanying processor.</summary>
    class GameEngine : Engine
    {
        #region Compiler override options
        /// <see cref="Engine"/>
        protected override void PreCompile()
        {
            // Add other references.
            LocalDlls = ["Ephemera.NScript"]; // the engine
            Usings.Add("Example.Script"); // script core Example.Script
        }

        /// <see cref="Engine"/>
        protected override void PostCompile()
        {
            if (Directives.TryGetValue("kustom", out string? value))
            {
                Console.WriteLine($"Script has {value}!");
            }
        }

        /// <see cref="Engine"/>
        protected override bool PreprocessLine(string sline, int lineNum, ScriptFile pcont)
        {
            // Check for any specials.
            if (sline.Trim() == "JUNK")
            {
                // Could do something useful with this.
                return true;
            }
            return false;
        }
        #endregion
    }

    /// <summary>Start here. TODOX slow startup?</summary>
    internal class Program
    {
        public static TimeIt tm = new();
        static void Main(string[] _)
        {
            tm.Snap("10");
            //await Engine.WarmupRoslyn();

            var app = new Example();
            var ret = app.Run();
            if (ret > 0)
            {
                Console.WriteLine($"App failed with {ret}");
            }

            tm.Snap("100");
            tm.Captures.ForEach(t => Console.WriteLine(t));

            Environment.Exit(ret);
        }
    }
}
