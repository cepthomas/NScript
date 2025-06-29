using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Ephemera.NScript;
//using Test;

namespace Ephemera.NScript.Test
{
    public class COMP_CORE : TestSuite
    {
        public override void RunSuite()
        {
            UT_INFO("Tests script compiler on a simple text block of code.");

            // Compile script.
            CompilerCore compiler = new();

            compiler.CompileText(code);

            UT_EQUAL(compiler.Results.Count, 3);

            //var lines = code.SplitByToken("\n");
            //compiler.Results.ForEach(res => UT_INFO($"{res.ResultType} {res.LineNumber} [{res.Message}]"));
            // >>>>
            //Suite COMP_CORE
            //Tests script compiler on a simple text block of code.
            //Info - 1[Compile text took 756 msec.]
            //Error 4[Unnecessary using directive.]
            //Error 2[Unnecessary using directive.]
        }

        readonly string code = @"//1
        using System;
        using System.Collections.Generic;
        using System.IO;
//5
        namespace Ephemera.NbotTest
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
    }

    public class COMP_APP : TestSuite
    {
        public override void RunSuite()
        {
            UT_INFO("Tests script compiler using example file.");
            UT_INFO("This demonstrates how a host application loads and runs scripts.");
            UT_STOP_ON_FAIL(true);

            var ok = true;

            // Compile script.
            MyCompiler compiler = new();
            compiler.CompileScript(Path.Combine("Variation999.sctest"));
            compiler.Results.ForEach(res => UT_INFO($"{res}"));

            UT_NOT_NULL(compiler.Script);

            //if (compiler.Script is null)
            //{
            //    // It failed.
            //    return;
            //}

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
                //ProcessScriptRuntimeError(ex);
                ok = false;
            }

            UT_TRUE(ok);
        }
    }
}
