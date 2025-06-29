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
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "COMP" };
            runner.RunSuites(cases);
            //var fn = Path.Combine(MiscUtils.GetSourcePath(), "out", "pnut_out.txt");
            //File.WriteAllLines(fn, runner.Context.OutputLines);
            //runner.Context.OutputLines.ForEach(l => Console.WriteLine(l));
            // Environment.Exit(0);
        }
    }
}
