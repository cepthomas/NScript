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
            var apiFile = Path.Combine(compiler.ScriptPath, "GameScriptApi.cs");
            compiler.CompileScript(scriptFile, apiFile);

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
                var t = compiler.CompiledScript.GetType();


                Type? tapi;
                while (t != null)
                {
                    if (t.Name == "GameScriptApi")
                    {
                        tapi = t; // as Ephemera.NScript.Example.GameScriptApi;
                    }
                    Console.WriteLine(t.ToString());
                    t = t.BaseType;
                }

                //UserScript.Game999
                //Ephemera.NScript.Example.GameScriptApi
                //System.Object


                //var aa = compiler.CompiledScript is GameScriptApi;


                //// Ephemera.NScript.Example.GameScriptApi


                //var script = compiler.CompiledScript as GameScriptApi; // {Game999.UserScript.Game999}

                ////script.PrintMessage += (_, msg) => Console.WriteLine(msg);


                //var res = script!.Setup();

                //// Run the game loop.
                //for (int i = 0; i < 10; i++)
                //{
                //    script.Move();
                //}


            }
            catch (Exception ex) //TODO1 handle like compiler errors.
            {
                Console.WriteLine($"Runtime Exception: {ex.Message}");
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
                compiler.Reports.ForEach(res => Console.WriteLine($"{res}"));
                return 1;
            }

            return 0;
        }
    }
}
