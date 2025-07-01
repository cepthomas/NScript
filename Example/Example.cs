using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ephemera.NBagOfTricks;
//using Ephemera.NScript.Example.Script;


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
            MyCompiler compiler = new() { ScriptPath = MiscUtils.GetSourcePath() };
            var scriptFile = Path.Combine(compiler.ScriptPath, "Variation999.scex");
            var apiFile = Path.Combine(compiler.ScriptPath, "MyScriptApi.cs");
            compiler.CompileScript(scriptFile, apiFile);

            if (compiler.CompiledScript is null)
            {
                // It failed.
                Console.WriteLine($"Compile Failed:");
                compiler.Results.ForEach(res => Console.WriteLine($"{res}"));
                return 1;
            }

            // Need exception handling to protect from user runtime script errors.
            try
            {
                // Init script.
                var script = compiler.CompiledScript as MyScriptApi;
                var res = script!.Setup();

                // Run the game loop.
                for (int i = 0; i < 10; i++)
                {
                    script.Move();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Compile Exception: {ex.Message}");
                Console.WriteLine($"{ex.StackTrace}");
            }
            return 0;
        }

        /// <summary>
        /// Run script compiler on a simple text block of code.
        /// </summary>
        /// <returns></returns>
        static int DoScriptText() // TODO1 remove/relocate this?
        {
            string code = @"

            using System;
            using System.Collections.Generic;
            using System.IO;

            namespace NScript.Test
            {
                public class Bag
                {
                    /// <summary>The file name.</summary>
                    string FileName { get; set; } = ""abcd"";

                    public Dictionary<string, object> Values { get; set; } = new();

                    public bool Valid => false;

                    #region HooHaa
                    public double GetDouble(string owner, string valname, double defval)
                    {
                        var ret = 123.45;
                        return ret;
                    }

                    public int GetInteger(string owner, string valname, int defval)
                    {
                        int ret = int.MinValue;
                        return ret;
                    }
                    #endregion
                }
                
                public record Person(string FirstName, string LastName);
            }";

            // Compile script.
            CompilerCore compiler = new();
            compiler.CompileText(code);

            if (compiler.CompiledScript is null)
            {
                // It failed.
                compiler.Results.ForEach(res => Console.WriteLine($"{res}"));
                return 1;
            }

            return 0;
        }
    }
}
