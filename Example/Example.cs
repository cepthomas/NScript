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
            GameCompiler compiler = new()
            {
                ScriptPath = ".",
                IgnoreWarnings = true,
                Namespace = "Example.Script" // same as ScriptBase.cs
            };

            var scriptFile = Path.Combine(compiler.ScriptPath, "Game999.csx");
            var baseFile = Path.Combine(compiler.ScriptPath, "ScriptBase.cs");

            // Namespace should be the same as ScriptBase.cs.
            compiler.CompileScript(scriptFile, "ScriptBase", [baseFile]);

            if (compiler.CompiledScript is null)
            {
                Console.WriteLine($"Compile failed:");
                compiler.Reports.ForEach(rep => Console.WriteLine($"{rep}"));
                return 1;
            }

            ///// Execute script. Needs exception handling to protect from user runtime script errors.
            try
            {
                // Init script.
                var inst = compiler.CompiledScript;
                var type = inst.GetType();

                // Reflection methods.
                var miInit = type.GetMethod("Init");
                var miSetup = type.GetMethod("Setup");
                var miMove = type.GetMethod("Move");
                var piTime = type.GetProperty("RealTime");

                // Delegates.
                var Init = (Func<TextWriter, int>)Delegate.CreateDelegate(typeof(Func<TextWriter, int>), inst, type.GetMethod("Init")!);
                var Setup = (Func<string, int, int, int>)Delegate.CreateDelegate(typeof(Func<string, int, int, int>), inst, type.GetMethod("Setup")!);
                var Move = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), inst, type.GetMethod("Move")!);
                var SetRealTime = (Action<double>)Delegate.CreateDelegate(typeof(Action<double>), inst, type.GetProperty("RealTime")!.GetSetMethod()!);
                var GetRealTime = (Func<double>)Delegate.CreateDelegate(typeof(Func<double>), inst, type.GetProperty("RealTime")!.GetGetMethod()!);

                // Run the game.
                Init(Console.Out);
                Setup("Here I am!!!", 60, 80);
                SetRealTime(500);
                // Using invoke:
                //miInit!.Invoke(inst, [Console.Out]);
                //miSetup!.Invoke(inst, ["Here I am!!!", 60, 80]);
                //piTime!.SetValue(inst, 100.0);

                for (int i = 0; i < 10; i++)
                {
                    Move();
                }

                // Examine effects.
                var ntime = GetRealTime();
                Console.WriteLine($"Finished at {ntime}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script runtime failed:");
                compiler.Reports.Clear();
                compiler.ProcessRuntimeException(ex);
                compiler.Reports.ForEach(rep => Console.WriteLine($"{rep}"));
                return 2;
            }

            return 0;
        }
    }

    /// <summary>The accompanying processor.</summary>
    class GameCompiler : CompilerCore
    {
        #region Compiler override options
        /// <see cref="CompilerCore"/>
        protected override void PreCompile()
        {
            // Add other references.
            LocalDlls = ["Ephemera.NScript"]; // the compiler
            Usings.Add("Example.Script"); // script core Example.Script
        }

        /// <see cref="CompilerCore"/>
        protected override void PostCompile()
        {
            if (Directives.TryGetValue("kustom", out string? value))
            {
                Console.WriteLine($"Script has {value}!");
            }
        }

        /// <see cref="CompilerCore"/>
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

    /// <summary>Start here.</summary>
    internal class Program
    {
        static void Main()
        {
            _ = Utils.WarmupRoslyn();

            //var dev = new Dev();
            //dev.Explore();
            //return; 

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
