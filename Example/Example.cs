using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


namespace Ephemera.NScript.Example
{
    class Example
    {
        /// <summary>
        /// Run script compiler using example file.
        /// This demonstrates how a host application loads and runs scripts.
        /// </summary>
        /// <returns>Exit code: 0=ok 1=compiler or syntax error 2=runtime error</returns>
        public int Run()
        {
            Program.tm.Snap("20");

            ///// Compile script with application options.
            GameEngine engine = new()
            {
                ScriptPath = ".", //MiscUtils.GetSourcePath(),
                IgnoreWarnings = true //false,
            };

            var scriptFile = Path.Combine(engine.ScriptPath, "Game999.csx");
            var baseFile = Path.Combine(engine.ScriptPath, "ScriptBase.cs");

            engine.CompileScript(scriptFile, "ScriptBase", [baseFile]);

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
            Usings.Add("NScript.Example.Script"); // script core
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
            // Check for my specials.
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
