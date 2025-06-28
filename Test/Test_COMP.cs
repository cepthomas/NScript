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


namespace Test
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

    public class COMP_CUSTOM : TestSuite
    {
        public override void RunSuite()
        {
            UT_INFO("Tests script compiler using example file.");

            // // Compile script.
            // CompilerCore compiler = new();

            //var startFolder = GetSourceDir();

            // compiler.CompileText(code);

            //UT_EQUAL(compiler.Results.Count, 1);

            bool ok = true;

            // Compile script.
            MyCompiler compiler = new();


            compiler.CompileScript(Path.Combine(MiscUtils.GetSourcePath(), "Varation999.sctest"));

            compiler.Results.ForEach(res => UT_INFO($"{res}"));
            //compiler.Results.ForEach(res => UT_INFO($"{res.ResultType}({res.LineNumber}) [{res.Message}]"));
            //>>>
            //Suite COMP_CUSTOM
            //Tests script compiler using example file.
            //Info NA(-1) [Compiling C:\Dev\Libs\ScriptCompiler\Test\Varation999.sctest.]
            //Error C:\Dev\Libs\ScriptCompiler\Test\Varation999.sctest(3) [Invalid Include: Include("Utils.sctest");]
            //Error NA(-1) [Exception: Could not find file 'C:\Dev\Libs\ScriptCompiler\Test\bin\net8.0-windows\win-x64\NAudio.dll'.]
            //Info NA(-1) [Compile script took 52 msec.]


            if (compiler.Script is null)
            {
                // It failed.
                return;
            }

            var script = compiler.Script as ScriptBase;


            UT_TRUE(ok);

            //// Log compiler results.
            //compiler.Results.ForEach(r =>
            //{
            //    string msg = r.SourceFile != "" ? $"{Path.GetFileName(r.SourceFile)}({r.LineNumber}): {r.Message}" : r.Message;
            //    switch (r.ResultType)
            //    {
            //        case CompileResultType.Error: _logger.Error(msg); break;
            //        case CompileResultType.Warning: _logger.Warn(msg); break;
            //        default: _logger.Info(msg); break;
            //    }
            //});

            // Need exception handling here to protect from user script errors.
            try
            {
                // Init shared vars.
                //InitRuntime();
                // void InitRuntime()
                // {
                //     if (_script is not null)
                //     {
                //         _script.Playing = chkPlay.Checked;
                //         _script.StepTime = _stepTime;
                //         _script.RealTime = (DateTime.Now - _startTime).TotalSeconds;
                //         _script.Tempo = (int)sldTempo.Value;
                //         _script.MasterVolume = sldVolume.Value;
                //     }
                // }

                var res = script.Setup(100, 200);

                // Do stuff with script...

                //script.CreatePlayer
            }
            catch (Exception ex)
            {
                //ProcessScriptRuntimeError(ex);
                ok = false;
            }

            //return ok;
        }
    }
}
