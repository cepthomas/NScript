using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.Windows.Forms;
using Ephemera.NBagOfTricks.PNUT;


namespace Ephemera.NScript.Example
{
    internal class Example
    {
        static void Main(string[] _)
        {
            DoScriptFile();
            //DoScriptText();
        }

        static int DoScriptFile()
        {
            Console.WriteLine("Run script compiler using example file.");
            Console.WriteLine("This demonstrates how a host application loads and runs scripts.");

            // Compile script.
            MyCompiler compiler = new();
            compiler.CompileScript("Variation999.sctest");

            if (compiler.Script is null)
            {
                // It failed.
                compiler.Results.ForEach(res => Console.WriteLine($"{res}"));
                return 1;
            }

            var script = compiler.Script as MyScriptApi;

            // Run the game loop.
            // Need exception handling here to protect from user script errors.
            try
            {
                var res = script!.Setup();

                for (int i = 0; i < 10; i++)
                {
                    script.Move();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script Error: {ex.Message}");
                //throw;
            }
            return 0;
        }


        static int DoScriptText()
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

            Console.WriteLine("Run script compiler on a simple text block of code.");

            // Compile script.
            CompilerCore compiler = new();

            compiler.CompileText(code);

            return 0;
        }
    }
}
