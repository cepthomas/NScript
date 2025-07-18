using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


namespace Ephemera.NScript
{
    public enum ReportType
    {
        Internal,   // Compiler error etc.
        Syntax,     // User script syntax error.
        Runtime,    // User script execution error.
    }

    public enum ReportLevel { None, Info, Warning, Error }

    /// <summary>General script result container.</summary>
    public class Report()
    {
        /// <summary>What kind.</summary>
        public ReportType ReportType { get; set; } = ReportType.Internal;

        /// <summary>What kind.</summary>
        public ReportLevel Level { get; set; } = ReportLevel.None;

        /// <summary>Original source file if available/pertinent.</summary>
        public string? SourceFileName { get; set; }

        /// <summary>Original source line number 1-based. -1 means inapplicable or unknown.</summary>
        public int SourceLineNumber { get; set; } = -1;

        /// <summary>Content.</summary>
        public string Message { get; set; } = "???";

        /// <summary>For humans.</summary>
        public override string ToString()
        {
            string slevel = Level switch
            {
                ReportLevel.None => "---",
                ReportLevel.Info => "INF",
                ReportLevel.Warning => "WRN",
                ReportLevel.Error => "ERR",
                _ => throw new NotImplementedException()
            };

            StringBuilder sb = new($"{slevel} {ReportType}: ");

            if (SourceFileName is not null)
            {
                sb.Append($"{SourceFileName}({SourceLineNumber}) ");
            }
            sb.Append($"[{Message}]");
            return sb.ToString();
        }
    }

    /// <summary>Parser file context class - one per original script source file.</summary>
    public class ScriptFile(string fn)
    {
        /// <summary>Original source file.</summary>
        public string SourceFileName { get; init; } = fn;

        /// <summary>Modified file to feed the compiler.</summary>
        public string GeneratedFileName { get; set; } = "???";

        /// <summary>The script code lines to feed the compiler.</summary>
        public List<string> GeneratedCode { get; set; } = [];

        /// <summary>key is GeneratedCode line number aka index, value is Source line number.</summary>
        public Dictionary<int, int> LineNumberMap { get; set; } = [];

        /// <summary>This is a top-level (not included) file.</summary>
        public bool TopLevel { get; set; } = false;

        /// <summary>Get caller source line number from generatted file line.</summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public int GetSourceLineNumber(int lineNum)
        {
            int ln = LineNumberMap.TryGetValue(lineNum, out int value) ? value : -1;
            return ln;
        }
    }

    /// <summary>Reporting hard errors.</summary>
    public class ScriptException() : Exception() { }

    public class Utils
    {
        /// <summary>
        /// From https://github.com/RickStrahl/Westwind.Scripting/blob/master/Westwind.Scripting/RoslynLifetimeManager.cs
        /// </summary>
        /// <returns></returns>
        public static Task WarmupRoslyn()
        {
            string code = @"
            using System;
            namespace WarmupRoslyn
            {
               public class Klass
               {
                   public void Go () { }
               }
            }";

            // Warm up Roslyn in the background
            return Task.Run(() =>
            {
                CompilerCore compiler = new() { Namespace = "WarmupRoslyn" };
                compiler.CompileText(code);
            });
        }
    }
}
