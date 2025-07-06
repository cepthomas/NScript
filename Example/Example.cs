using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Ephemera.NBagOfTricks;


namespace NScript.Example
{
    class Example
    {
        /// <summary>
        /// Run script compiler using example file.
        /// This demonstrates how a host application loads and runs scripts.
        /// </summary>
        public int Run()
        {
            ///// Compile script with application options.
            GameCompiler compiler = new()
            {
                ScriptPath = MiscUtils.GetSourcePath(),
                IgnoreWarnings = false,
            };

            var scriptFile = Path.Combine(compiler.ScriptPath, "Game999.csx");
            var baseFile = Path.Combine(compiler.ScriptPath, "GameScriptBase.cs");

            compiler.CompileScript(scriptFile, "GameScriptBase", [baseFile]);

            if (compiler.CompiledScript is null)
            {
                Console.WriteLine($"Compile Failed:");
                compiler.Reports.ForEach(rep => Console.WriteLine($"{rep}"));
                return 1;
            }

            ///// Execute script. Needs exception handling to protect from user runtime script errors.
            try
            {
                // Init script.
                var script = compiler.CompiledScript;
                var scriptType = script.GetType();

                // Cache methods.
                var methodInit = scriptType.GetMethod("Init");
                var methodSetup = scriptType.GetMethod("Setup");
                var methodMove = scriptType.GetMethod("Move");

                // Run the game.
                methodInit.Invoke(script, [Console.Out]);
                methodSetup.Invoke(script, ["Here I am!!!", "too many"]);

                for (int i = 0; i < 10; i++)
                { 
                    methodMove.Invoke(script, []);  
                }
            }
            catch (Exception ex)
            {
                compiler.HandleRuntimeException(ex);
                return 2;
            }

            return 0;
        }
    }

    /// <summary>The accompanying compiler.</summary>
    class GameCompiler : CompilerCore
    {
        #region Compiler override options - see base class for doc
        protected override void PreCompile()
        {
            // Add other references.
            LocalDlls = ["NScript"];
            Usings.Add("NScript.Example");
        }

        protected override void PostCompile()
        {
            if (Directives.TryGetValue("kustom", out string? value))
            {
                ReportInternal(ReportLevel.Info, $"Script has {value}!");
            }
        }

        protected override bool PreprocessLine(string sline, ScriptFile pcont)
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

    /// <summary>Start here.</summary>
    internal class Program
    {
        static void Main(string[] _)
        {
            Environment.Exit(new Example().Run());
        }
    }
}
