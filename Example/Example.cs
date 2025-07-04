using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ephemera.NBagOfTricks;


namespace Ephemera.NScript.Example
{
    internal class Example
    {
        /// <summary>
        /// Start here.
        /// </summary>
        /// <param name="_">Args</param>
        static void Main(string[] _)
        {
            DoScriptFile();
            //DoScriptText();
        }

        /// <summary>
        /// Run script compiler using example file.
        /// This demonstrates how the host application loads and runs scripts.
        /// </summary>
        /// <returns></returns>
        static int DoScriptFile()
        {
            // Compile script.
            GameCompiler compiler = new()
            {
                ScriptPath = MiscUtils.GetSourcePath(),
                IgnoreWarnings = false,
            };

            var scriptFile = Path.Combine(compiler.ScriptPath, "Game999.csx");
            var baseFile = Path.Combine(compiler.ScriptPath, "GameScriptBase.cs");
            compiler.CompileScript(scriptFile, baseFile);

            if (compiler.CompiledScript is null)
            {
                // It failed.
                Console.WriteLine($"Compile Failed:");
                compiler.Reports.ForEach(res => Console.WriteLine($"{res}"));
                return 1;
            }

            // Need exception handling to protect from user runtime script errors.
            try
            {
                // Init script.
                //var script = compiler.CompiledScript;


                //var aa = script is GameScriptBase;
                // Ephemera.NScript.Example.GameScriptBase

                //var script = compiler.CompiledScript as GameScriptBase; // {Game999.UserScript.Game999}
                var script = (GameScriptBase)compiler.CompiledScript; // {Game999.UserScript.Game999}
                //script.PrintMessage += (_, msg) => Console.WriteLine(msg);


                var res = script!.Setup("TODO1");

                // Run the game loop.
                for (int i = 0; i < 10; i++)
                {
                   script.Move();
                }
            }
            catch (Exception ex) //TODO1 handle like compiler errors.
            {
                Console.WriteLine($"Runtime Exception: {ex.Message}");
            }
            return 0;
        }
    }
}
