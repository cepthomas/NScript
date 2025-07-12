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
            compiler.CompileScript(scriptFile, [baseFile]);

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
                var script = compiler.CompiledScript;
                var scriptType = script.GetType();

                // Cache accessors.
                var miInit = scriptType.GetMethod("Init");
                var miSetup = scriptType.GetMethod("Setup");
                var miMove = scriptType.GetMethod("Move");
                var piTime = scriptType.GetProperty("RealTime");
                // delegate flavor
                //var delMove = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), script, miMove);

                // Run the game.
                miInit.Invoke(script, [Console.Out]);
                miSetup.Invoke(script, ["Here I am!!!", 60, 80]);
                piTime.SetValue(script, 100.0);

                for (int i = 0; i < 10; i++)
                {
                    miMove.Invoke(script, []);
                    //delMove();
                }
                // Examine effects.
                var ntime = piTime.GetValue(script);
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

        /// <summary>Like Run() but used to investigate reflection options.</summary>
        /// <returns></returns>
        public void Probe()
        {
            ///// Compile script with application options.
            GameCompiler compiler = new()
            {
                ScriptPath = ".",
                IgnoreWarnings = true,
                Namespace = "DontCare"
            };

            string code = @"
            using System;
            namespace DontCare
            {
                public class Klass
                {
                    public double RealTime { get; set; } = 123.45;
                    public int Dev(string s) { RealTime += 1.0; return s.Length; }
                }
            }";

            var assy = compiler.CompileText(code);
            object? inst = null;
            Type? type = null;

            foreach (Type t in assy.GetTypes())
            {
                if (t is not null && t.Name == "Klass")
                {
                    type = t;
                }
            }
            inst = Activator.CreateInstance(type);

            // Automate/generate these?
            // try: UnsafeAccessor  [MemoryDiagnoser]/[Benchmark]  in bench.cs
            var miDev = type.GetMethod("Dev");
            var piTime = type.GetProperty("RealTime");
            var delDev = (Func<string, int>)Delegate.CreateDelegate(typeof(Func<string, int>), inst, miDev);

            List<long> invoked = [];
            List<long> delegated = [];
            List<long> prop = [];

            for (int i = 0; i < 10; i++)
            {
                var start = Stopwatch.GetTimestamp();
                miDev.Invoke(inst, ["xxx"]);
                invoked.Add(Stopwatch.GetTimestamp() - start);

                start = Stopwatch.GetTimestamp();
                delDev("xxx");
                delegated.Add(Stopwatch.GetTimestamp() - start);

                start = Stopwatch.GetTimestamp();
                piTime.GetValue(inst);
                prop.Add(Stopwatch.GetTimestamp() - start);
            }

            // Examine effects.
            // properties: using the results of the GetGetMethod and GetSetMethod methods of PropertyInfo.
            var ntime = piTime.GetValue(inst);

            for (int i = 0; i < invoked.Count; i++)
            {
                Console.WriteLine($"iter{i} invoked:{invoked[i]} delegated:{delegated[i]} prop:{prop[i]}");
            }
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

            var app = new Example();
            app.Probe(); return;
            var ret = app.Run();
            if (ret > 0)
            {
                Console.WriteLine($"App failed with {ret}");
            }

            Environment.Exit(ret);
        }
    }
}
