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



// TODOX add example with separate script.csproj like neb and nproc.


namespace Example
{
    class Example
    {
        /// <summary>This demonstrates how a host application:
        /// - compiles a script
        /// - loads the assembly into memory
        /// - runs script using reflection
        /// - runs script using direct invocation
        /// </summary>
        /// <returns>Exit code: 0=ok 1=compiler or syntax error 2=runtime error</returns>
        public int Run()
        {
            ////////////////// By reflection //////////////////
            // Compile script with application options. GameCompiler is this app-specific flavor.
            // It's defined below, or could be in a separate file.
            GameCompiler compiler = new()
            {
                IgnoreWarnings = true,                  // to taste
                Namespace = "Example.Script",           // same as ScriptCore.cs
                BaseClassName = "ScriptCore",           // same as ScriptCore.cs
            };

            // Components of executable script.
            var scriptPath = MiscUtils.GetSourcePath();
            var scriptFile = Path.Combine(scriptPath, "Script", "Game999.csx");
            var coreFile = Path.Combine(scriptPath, "Script", "ScriptCore.cs");

            // Run the compiler. Add the script core file name.
            compiler.CompileScript(scriptFile, [coreFile]);

            // What happened?
            if (compiler.CompiledScript is null)
            {
                Program.Print($"Compile failed:");
                compiler.Reports.ForEach(rep => Program.Print($"{rep}"));
                return 1;
            }

            // OK here. Load and execute script. Needs exception handling to detect user runtime script errors.
            try
            {
                // Init script.
                var inst = compiler.CompiledScript;
                var type = inst.GetType();

                // Could use simple reflection methods:
                // var miInit = type.GetMethod("Init");
                // var miSetup = type.GetMethod("Setup");
                // var miMove = type.GetMethod("Move");
                // var piTime = type.GetProperty("RealTime");

                // But delegates are better.
                var Init = (Func<TextWriter, int>)Delegate.CreateDelegate(typeof(Func<TextWriter, int>), inst, type.GetMethod("Init")!);
                var Setup = (Func<string, int, int, int>)Delegate.CreateDelegate(typeof(Func<string, int, int, int>), inst, type.GetMethod("Setup")!);
                var Move = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), inst, type.GetMethod("Move")!);
                var SetRealTime = (Action<double>)Delegate.CreateDelegate(typeof(Action<double>), inst, type.GetProperty("RealTime")!.GetSetMethod()!);
                var GetRealTime = (Func<double>)Delegate.CreateDelegate(typeof(Func<double>), inst, type.GetProperty("RealTime")!.GetGetMethod()!);

                // Run the game.
                Init(Console.Out);
                Setup("Here I am!!!", 60, 80);
                SetRealTime(500);
                // Using simple reflection:
                //miInit!.Invoke(inst, [Console.Out]);
                //miSetup!.Invoke(inst, ["Here I am!!!", 60, 80]);
                //piTime!.SetValue(inst, 100.0);

                for (int i = 0; i < 10; i++)
                {
                    Move();
                }

                // Examine effects.
                var ntime = GetRealTime();
                Program.Print($"Finished at {ntime}");
            }
            catch (Exception ex)
            {
                Program.Print($"Script runtime failed:");
                compiler.Reports.Clear();
                compiler.ProcessRuntimeException(ex);
                compiler.Reports.ForEach(rep => Program.Print($"{rep}"));
                return 2;
            }

            ////////////////// By direct invocation //////////////////

            // Add the known assembly.
            compiler.LocalDlls.Add("Example.Script");

            // Run the compiler. Note >>> coreFile is not included in build.
            compiler.CompileScript(scriptFile, [coreFile]);

            // What happened?
            if (compiler.CompiledScript is null)
            {
                Program.Print($"Compile failed:");
                compiler.Reports.ForEach(rep => Program.Print($"{rep}"));
                return 1;
            }

            // OK here. Load and execute script. Needs exception handling to detect user runtime script errors.
            try
            {
                // Cast to known.
                var script = compiler.CompiledScript as ScriptCore;

                // Run the game.
                script.Init(Console.Out);
                script.Setup("Here I am!!!", 60, 80);
                script.RealTime = 500;

                for (int i = 0; i < 10; i++)
                {
                    script.Move();
                }

                // Examine effects.
                var ntime = script.RealTime;
                Program.Print($"Finished at {ntime}");
            }
            catch (Exception ex)
            {
                Program.Print($"Script runtime failed:");
                compiler.Reports.Clear();
                compiler.ProcessRuntimeException(ex);
                compiler.Reports.ForEach(rep => Program.Print($"{rep}"));
                return 2;
            }

            return 0;
        }
    }

    /// <summary>The compiler specific to the application.</summary>
    class GameCompiler : CompilerCore
    {
        #region Compiler override options
        /// <summary>Called before compiler starts.</summary>
        /// <see cref="CompilerCore"/>
        protected override void PreCompile()
        {
            // Add references.
            SystemDlls =
            [
                "System",
                "System.Private.CoreLib",
                "System.Runtime",
                "System.Collections",
            ];

            LocalDlls =
            [
                "Ephemera.NBagOfTricks",
                "Ephemera.NScript",
                //"Example.Script" add
            ];

            Usings =
            [
                "System.Collections.Generic",
                "System.Text",
            ];
        }

        /// <summary>Called after compiler finished.</summary>
        /// <see cref="CompilerCore"/>
        protected override void PostCompile()
        {
            // Check for our app-specific directives.
            Directives
                .Where(d => d.dirname == "kustom")
                .ForEach(dk => Program.Print($"Script has a {dk.dirval}!"));
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


    /// <summary>Example starts here.</summary>
    internal class Program
    {
        static void Main()
        {
            //_ = Utils.WarmupRoslyn();

            var app = new Example();
            var ret = app.Run();
            if (ret > 0)
            {
                Program.Print($"App failed with {ret}");
            }

            Environment.Exit(ret);
        }

        /// <summary>Tell the user something.</summary>
        public static void Print(string msg)
        {
            Console.WriteLine($">>>: {msg}");
        }
    }
}
