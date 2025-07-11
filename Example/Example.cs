using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;


namespace Example
{
    class Example
    {
        /// <summary>This demonstrates how a host application loads and runs scripts.</summary>
        /// <returns>Exit code: 0=ok 1=compiler or syntax error 2=runtime error</returns>
        public int Run()
        {
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

            ///// Execute script. Needs exception handling to protect from user runtime script errors.
            try
            {
                // Init script.
                var script = engine.CompiledScript;
                var scriptType = script.GetType();

                // Cache accessors.
                var methodInit = scriptType.GetMethod("Init");
                var methodSetup = scriptType.GetMethod("Setup");
                var methodMove = scriptType.GetMethod("Move");
                var propTime = scriptType.GetProperty("RealTime");

                // Run the game.
                methodInit.Invoke(script, [Console.Out]);
                methodSetup.Invoke(script, ["Here I am!!!", 60, 80]);
                propTime.SetValue(script, 100.00);

                for (int i = 0; i < 10; i++)
                {
                    methodMove.Invoke(script, []);
                }

                var ntime = propTime.GetValue(script);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script runtime failed:");
                engine.Reports.Clear();
                engine.ProcessRuntimeException(ex);
                engine.Reports.ForEach(rep => Console.WriteLine($"{rep}"));
                return 2;
            }

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
        static void Main(string[] _)
        {
            //await Engine.WarmupRoslyn();

            var app = new Example();
            var ret = app.Run();
            if (ret > 0)
            {
                Console.WriteLine($"App failed with {ret}");
            }

            Environment.Exit(ret);
        }
    }
}
