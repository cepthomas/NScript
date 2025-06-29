using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.Windows.Forms;
using Ephemera.NBagOfTricks.PNUT;


// namespace Ephemera.ScriptCompiler.Test

namespace Ephemera.NScript.Test
{
    internal class Program
    {
        static void Main(string[] _)
        {
            //DoScriptText();

            DoScriptFile();
        }

        static void DoScriptText()
        {
            string code = @"//1
        using System;
        using System.Collections.Generic;
        using System.IO;
//5
        namespace Ephemera.Test
        {
            public class Bag
            {
                /// <summary>The file name.</summary> //10
                string FileName { get; set; } = ""abcd"";

                public Dictionary<string, object> Values { get; set; } = new();
                //public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

                public bool Valid => false;
                //public bool Valid { get; set; } = false;

                #region HooHaa
                public double GetDouble(string owner, string valname, double defval) //20
                {
                    var ret = 123.45;
                    return ret;
                }

                public int GetInteger(string owner, string valname, int defval)
                {
                    int ret = int.MinValue;
                    return ret;
                }// 30
                #endregion
            }
            
            public record Person(string FirstName, string LastName);
        }";

            Console.WriteLine("Run script compiler on a simple text block of code.");

            // Compile script.
            CompilerCore compiler = new();

            compiler.CompileText(code);

            //compiler.Results;
        }

        static void DoScriptFile()
        {
            Console.WriteLine("Run script compiler using example file.");
            Console.WriteLine("This demonstrates how a host application loads and runs scripts.");

            // Compile script.
            MyCompiler compiler = new();
            compiler.CompileScript(Path.Combine("Variation999.sctest"));
            compiler.Results.ForEach(res => Console.WriteLine($"{res}"));

            if (compiler.Script is null)
            {
                // It failed.
                return;
            }

            var script = compiler.Script as ScriptBase;

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
                throw;
            }
        }
    }
}
