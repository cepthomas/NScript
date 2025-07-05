using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Ephemera.NBagOfTricks;



namespace NScript
{
    #region Types
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
        public ReportType ReportType { get; set; }

        /// <summary>What kind.</summary>
        public ReportLevel Level { get; set; }

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

        /// <summary></summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public int GetSourceLineNumber(int lineNum)
        {
            int ln = LineNumberMap.TryGetValue(lineNum, out int value) ? value : -1;
            return ln;
        }
    }
    #endregion


    #region TODO1 these?

    // // https://carljohansen.wordpress.com/2020/05/09/compiling-expression-trees-with-roslyn-without-memory-leaks-2/
    // In researching the problem I saw hints of a potential solution in a new feature of .NET Core 3 called 
    // “collectible AssemblyLoadContexts”.  AssemblyLoadContext has been around for a long time, but 
    // collectible ALCs, with an Unload method, are new.
    // > SearchFilterCompiler.cs
    
    /*
    /// <summary>
    /// This helper can help start up Roslyn before first call so that there's no
    /// long startup delay for first script execution and you can also optionally
    /// shut Roslyn down and kill the VBCSCompiler that otherwise stays loaded
    /// even after shutting down your application.
    /// </summary>
    public class RoslynLifetimeManager
    {
        /// <summary>
        /// Run a script execution asynchronously in the background to warm up Roslyn.
        /// Call this during application startup or anytime before you run the first
        /// script to ensure scripts execute quickly.
        ///
        /// Although this method returns `Task` so it can be tested
        /// for success, in applications you typically will call this
        /// without `await` on the result task and just let it operate
        /// in the background.
        /// </summary>
        public static Task<bool> WarmupRoslyn()
        {
            // warm up Roslyn in the background
            return Task.Run(() =>
            {
                var script = new CSharpScriptExecution();
                script.AddDefaultReferencesAndNamespaces();
                var result = script.ExecuteCode("int x = 1; return x;", null);

                return result is 1;
            });
        }

        /// <summary>
        /// Call this method to shut down the VBCSCompiler if our
        /// application started it.
        /// </summary>
        public static void ShutdownRoslyn(string appStartupPath = null)
        {
            if (string.IsNullOrEmpty(appStartupPath))
                appStartupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var processes = Process.GetProcessesByName("VBCSCompiler");
            foreach (var process in processes)
            {
                // only shut down 'our' VBCSCompiler
                var fn = GetMainModuleFileName(process);
                if (fn.Equals(appStartupPath, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // ignore kill operation errors
                    }
                }
            }
        }


        [DllImport("Kernel32.dll")]
        private static extern bool QueryFullProcessImageName(
            [In] IntPtr hProcess,
            [In] uint dwFlags,
            [Out] StringBuilder lpExeName,
            [In, Out] ref uint lpdwSize);

        public static string GetMainModuleFileName(Process process)
        {
            var fileNameBuilder = new StringBuilder(1024);
            uint bufferLength = (uint) fileNameBuilder.Capacity + 1;
            return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength)
                ? fileNameBuilder.ToString()
                : null;
        }

    }
    */
    #endregion
   
}
