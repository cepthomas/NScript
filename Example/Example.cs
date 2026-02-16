using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;
using Example.Script;


namespace Example
{
    class Example
    {
        /// <summary>This demonstrates how a host application:
        /// - compiles a script
        /// - loads the assembly into memory
        /// - runs script using reflection
        /// </summary>
        /// <returns>Exit code: 0=ok 1=compiler or syntax error 2=runtime error</returns>
        public int RunByReflection()
        {
            Print($"===== Running Game1 by reflection =====");

            GameCompiler compiler = new()
            {
                IgnoreWarnings = false,         // to taste
                Namespace = "Example.Script",   // same as Api.cs
                BaseClassName = "Api",          // same as Api.cs
            };

            // Locate files of interest.
            var scriptFile = Path.Combine(MiscUtils.GetSourcePath(), "Script", "Game1.csx");
            var apiFile = Path.Combine(MiscUtils.GetSourcePath(), "Script", "Api.cs");

            // Run the compiler.
            compiler.CompileScript(scriptFile, [apiFile]);

            // What happened?
            if (compiler.CompiledScript is null)
            {
                Print($"Compile failed:");
                compiler.Reports.ForEach(rep => Print($"{rep}"));
                return 1;
            }

            // Load and execute script. Exception handling to detect user runtime script errors.
            try
            {
                // Init script.
                var inst = compiler.CompiledScript;
                var type = inst.GetType();

                // Could use simple reflection or delegates.
                bool useDelegate = true;

                if (useDelegate)
                {
                    var Init = (Func<TextWriter, int>)Delegate.CreateDelegate(typeof(Func<TextWriter, int>), inst, type.GetMethod("Init")!);
                    var Setup = (Func<string, int, int, int>)Delegate.CreateDelegate(typeof(Func<string, int, int, int>), inst, type.GetMethod("Setup")!);
                    var Move = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), inst, type.GetMethod("Move")!);
                    var SetRealTime = (Action<double>)Delegate.CreateDelegate(typeof(Action<double>), inst, type.GetProperty("RealTime")!.GetSetMethod()!);
                    var GetRealTime = (Func<double>)Delegate.CreateDelegate(typeof(Func<double>), inst, type.GetProperty("RealTime")!.GetGetMethod()!);

                    // Run the game.
                    Init(Console.Out);
                    Setup("Welcome to game 1 with delegates", 60, 80);
                    SetRealTime(500);

                    for (int i = 0; i < 10; i++)
                    {
                        Move();
                    }

                    Print($"Finished at {GetRealTime()}");
                }
                else // simple reflection
                {
                    var miInit = type.GetMethod("Init");
                    var miSetup = type.GetMethod("Setup");
                    var miMove = type.GetMethod("Move");
                    var piTime = type.GetProperty("RealTime");

                    // Run the game.
                    miInit!.Invoke(inst, [Console.Out]);
                    miSetup!.Invoke(inst, ["Welcome to game 1 with invoke", 100, 200]);
                    piTime!.SetValue(inst, 200.0);

                    for (int i = 0; i < 10; i++)
                    {
                        miMove!.Invoke(inst, []);
                    }

                    Print($"Finished at fake time {piTime!.GetValue(inst)}");
                }
            }
            catch (Exception ex)
            {
                Print($"Script runtime failed:");
                compiler.Reports.Clear();
                compiler.ProcessRuntimeException(ex);
                compiler.Reports.ForEach(rep => Print($"{rep}"));
                return 2;
            }

            return 0;
        }

        /// <summary>This demonstrates how a host application:
        /// - compiles a script
        /// - loads the assembly into memory
        /// - runs script using late binding
        /// </summary>
        /// <returns>Exit code: 0=ok 1=compiler or syntax error 2=runtime error</returns>
        public int RunByBinding()
        {
            Print($"===== Running Game2 by late binding =====");

            GameCompiler compiler = new()
            {
                IgnoreWarnings = false,         // to taste
                Namespace = "Example.Script",   // same as Api.cs
                BaseClassName = "Api",          // same as Api.cs
            };
            // Add the known assemblies.
            compiler.LocalDlls.Add("Example.Script");

            // Locate files of interest. Does not include Api.cs!
            var scriptFile = Path.Combine(MiscUtils.GetSourcePath(), "Script", "Game2.csx");


            // Run the compiler.
            compiler.CompileScript(scriptFile);

            // What happened?
            if (compiler.CompiledScript is null)
            {
                Print($"Compile failed:");
                compiler.Reports.ForEach(rep => Print($"{rep}"));
                return 1;
            }

            // Load and execute script. Exception handling to detect user runtime script errors.
            try
            {
                // Cast to known.
                var script = compiler.CompiledScript as Api;

                // Run the game.
                script!.Init(Console.Out);
                script.Setup("Welcome to game 2 with late binding", 55, 88);
                script.RealTime = 500;

                for (int i = 0; i < 10; i++)
                {
                    script.Move();
                }

                Print($"Finished at fake time {script.RealTime}");
            }
            catch (Exception ex)
            {
                Print($"Script runtime failed:");
                compiler.Reports.Clear();
                compiler.ProcessRuntimeException(ex);
                compiler.Reports.ForEach(rep => Print($"{rep}"));
                return 2;
            }

            return 0;
        }

        /// <summary>Tell the user something.</summary>
        void Print(string msg)
        {
            Console.WriteLine($">>> {msg}");
        }
    }

    /// <summary>The compiler specific to the application.</summary>
    class GameCompiler : CompilerCore
    {
        public GameCompiler()
        {
            // Add references.
            SystemDlls = ["System", "System.Private.CoreLib", "System.Runtime", "System.Collections"];
            LocalDlls = ["Ephemera.NBagOfTricks", "Ephemera.NScript"];
            Usings = ["System.Collections.Generic", "System.Text"];
        }

        #region Compiler override options
        /// <summary>Called before compiler starts.</summary>
        /// <see cref="CompilerCore"/>
        protected override void PreCompile() { }

        /// <summary>Called after compiler finished.</summary>
        /// <see cref="CompilerCore"/>
        protected override void PostCompile()
        {
            // Check for our app-specific directives.
            Directives
                .Where(d => d.dirname == "kustom")
                .ForEach(dk => Console.WriteLine($"Script has a {dk.dirval}!"));
        }

        /// <summary>Called for each line in the source file before compiling.</summary>
        /// <param name="sline">Trimmed line</param>
        /// <param name="pcont">File context</param>
        /// <param name="lineNum">Source line number may be useful (1-based)</param>
        /// <returns>True if derived class took care of this</returns>
        /// <see cref="CompilerCore"/>
        protected override bool PreprocessLine(string sline, int lineNum, ScriptFile pcont)
        {
            // Check for anything specific to this flavor of script.
            if (sline.Contains("JUNK"))
            {
                // Could do something meaningful with this.
                return true;
            }
            return false;
        }
        #endregion
    }
}
