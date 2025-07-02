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


// https://carljohansen.wordpress.com/2020/05/09/compiling-expression-trees-with-roslyn-without-memory-leaks-2/

namespace Ephemera.NScript
{
    #region Types
    /// <summary>Category for things reported to user.</summary>
    public enum ReportType
    {
        None,       // Ignore.
        Info,       // Something to tell the user.
        Warning,    // Compiler warning. TODO1 useful?
        Error,      // Compiler error.
        Syntax,     // Script syntax error - user
        //Runtime,     // Script execution error - user
        //Other       // Custom use.
    }

    /// <summary>General script result container.</summary>
    /// <remarks>Convenience constructor.</remarks>
    public class Report()//ReportType resultType, string msg)
    {
        /// <summary>What kind.</summary>
        public ReportType ReportType { get; set; }// = resultType;

        /// <summary>Original source file if available/pertinent.</summary>
        public string? SourceFileName { get; set; }

        /// <summary>Original source line number 1-based. -1 means inapplicable or unknown.</summary>
        public int SourceLineNumber { get; set; } = -1;

        /// <summary>Content.</summary>
        public string Message { get; set; } = "???";

        /// <summary>For humans.</summary>
        public override string ToString()
        {
            StringBuilder sb = new($"{ReportType}: ");
            if (SourceFileName is not null)
            {
                sb.Append($"{SourceFileName}({SourceLineNumber}) ");
            }
            sb.Append($"[{Message}]");
            return sb.ToString();
        }
    }

    /// <summary>Parser file context class - one per original source file.</summary>
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

    /// <summary>Parses/compiles script file(s).</summary>
    public class CompilerCore
    {
        #region Properties
        /// <summary>Client option.</summary>
        public bool IgnoreWarnings { get; set; } = false;

        /// <summary>Client may need to tell us this for Include path.</summary>
        public string ScriptPath { get; set; } = "";

        /// <summary>Default system dlls. Client can add or subtract.</summary>
        public List<string> SystemDlls { get; } =
        [
            "System",
            "System.Private.CoreLib",
            "System.Runtime",
            "System.IO",    
            "System.Collections",
            "System.Linq"
        ];

        /// <summary>Additional using statements not supplied by core dlls.</summary>
        public List<string> Usings { get; set; } =
        [
            "System.Collections.Generic",
            "System.Text"
        ];

        /// <summary>App dlls supplied by app compiler.</summary>
        public List<string> LocalDlls { get; set; } = [];

        /// <summary>The compiled script.</summary>
        public object? CompiledScript { get; set; } = null;

        /// <summary>Accumulated errors and other bits of information - for user presentation.</summary>
        public List<Report> Reports { get; } = [];

        /// <summary>All active script source files. Provided so client can monitor for external changes. TODO1 used?</summary>
        public IEnumerable<string> SourceFiles { get { return [.. _scriptFiles.Select(f => f.SourceFileName)]; } }

        /// <summary>Compile products are here.</summary>
        public string TempDir { get; set; } = "???";
        #endregion

        #region Fields
        /// <summary>Script info.</summary>
        string _scriptName = "???";

        /// <summary>Script api.</summary>
        string _apiName = "???";

        /// <summary>Products of file preprocess.</summary>
        readonly List<ScriptFile> _scriptFiles = [];

        /// <summary>Other files to compile.</summary>
        readonly List<string> _plainFiles = [];

        /// <summary>Add the class wrapper if not in script.</summary>
        readonly bool _addWrapper = false; // TODO1? never used - maybe always add
        #endregion

        #region Overrides for derived classes to hook
        /// <summary>Called before compiler starts.</summary>
        public virtual void PreCompile() { }

        /// <summary>Called after compiler finished.</summary>
        public virtual void PostCompile() { }

        /// <summary>Called for each line in the source file before compiling.</summary>
        /// <param name="sline">Trimmed line</param>
        /// <param name="pcont">File context</param>
        /// <returns>True if handled</returns>
        public virtual bool PreprocessLine(string sline, ScriptFile pcont) { return false; }
        #endregion

        #region Public functions
        /// <summary>Run the compiler on a script file.</summary>
        /// <param name="scriptfn">Fully qualified path to main file.</param>
        /// <param name="apifn">Fully qualified path to api file.</param>
        public void CompileScript(string scriptfn, string apifn) // TODO1 combine with CompileText()?
        {
            // Reset everything.
            CompiledScript = null;
            Reports.Clear();
            _scriptFiles.Clear();
            _plainFiles.Clear();

            try
            {
                DateTime startTime = DateTime.Now;

                // Add the api file.
                _plainFiles.Add(apifn);
                _apiName = Path.GetFileNameWithoutExtension(apifn);

                PreCompile();

                // Get and sanitize the script name.
                _scriptName = Path.GetFileNameWithoutExtension(scriptfn);
                StringBuilder sb = new();
                _scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
                _scriptName = sb.ToString();
                var dir = Path.GetDirectoryName(scriptfn);

                // Process the source files into something that can be compiled.
                ScriptFile pcont = new(scriptfn);
                PreprocessFile(pcont); // recursive function

                // Compile the processed files.
                CompiledScript = Compile(dir!);

                RecordReport(ReportType.Info, $"Compiled script: {(DateTime.Now - startTime).Milliseconds} msec.");

                PostCompile();
            }
            catch (Exception ex)
            {
                RecordReport(ReportType.Error, $"Compile exception: {ex}.");
            }
        }

        /// <summary>
        /// Run the compiler on a simple text block.
        /// </summary>
        /// <param name="text">Text to compile.</param>
        public void CompileText(string text) // TODO1 do something with this + test code.
        {
            DateTime startTime = DateTime.Now; // for metrics

            List<SyntaxTree> trees = [];

            // Build a syntax tree.
            CSharpParseOptions popts = new();
            SyntaxTree tree = CSharpSyntaxTree.ParseText(text, popts);
            trees.Add(tree);

            // We now build up a list of references needed to compile the code.
            var references = new List<MetadataReference>();
            // System stuff location.
            var dotnetStore = Path.GetDirectoryName(typeof(object).Assembly.Location);
            // Project refs like nuget.
            var localStore = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // System dlls. { "System", "System.Private.CoreLib", "System.Runtime", "System.Collections", "System.Linq" };
            SystemDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll"))));

            // Local dlls. none default.
            LocalDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll"))));

            // Emit to stream.
            var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create($"{_scriptName}.dll", trees, references, copts);
            var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            RecordReport(ReportType.Info, $"CompilerCore: text took {(DateTime.Now - startTime).Milliseconds} msec.");

            if (result.Success)
            {
                // Load into currently running assembly.
                var assy = Assembly.Load(ms.ToArray());
                var types = assy.GetTypes();
            }

            // Collect results.
            foreach (var diag in result.Diagnostics)
            {
                //var msg = diag.GetMessage();
                var lineNum = diag.Location.GetLineSpan().StartLinePosition.Line + 1;
                RecordReport(Translate(diag.Severity), diag.GetMessage(), "TODO1 src file name", lineNum);
            }
        }
        #endregion

        #region Private functions
        /// <summary>The actual compiler driver.</summary>
        /// <param name="baseDir">Fully qualified path to main file.</param>
        /// <returns>Compiled script</returns>
        object? Compile(string baseDir)
        {
            object? script = null;

            try // many ways to go wrong...
            {
                // Create temp output area and/or clean it.
                TempDir = Path.Combine(baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                // Assemble constituents.
                List<SyntaxTree> trees = [];

                // Write the generated source files to temp build area.
                foreach (var tocomp in _scriptFiles)//.Keys)
                {
                    if (tocomp.GeneratedCode.Count > 0)
                    {
                        string fullpath = Path.Combine(TempDir, tocomp.GeneratedFileName);
                        File.WriteAllLines(fullpath, tocomp.GeneratedCode);

                        // Build a syntax tree.
                        string code = File.ReadAllText(fullpath);
                        CSharpParseOptions popts = new();
                        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, popts, tocomp.GeneratedFileName);
                        trees.Add(tree);
                    }
                }

                foreach (var fn in _plainFiles)
                {
                    // Build a syntax tree.
                    string code = File.ReadAllText(fn);
                    CSharpParseOptions popts = new();
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(code, popts, fn);
                    trees.Add(tree);
                }

                // Build up a list of references needed to compile the code.
                var references = new List<MetadataReference>();
                // System stuff location.
                var dotnetStore = Path.GetDirectoryName(typeof(object).Assembly.Location);
                // Project refs like nuget.
                var localStore = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // System dlls.
                SystemDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll"))));

                // Local dlls.
                LocalDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll"))));

                // Emit to stream.
                var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                var compilation = CSharpCompilation.Create($"{_scriptName}.dll", trees, references, copts);

                var ms = new MemoryStream();
                EmitResult result = compilation.Emit(ms);

                if (result.Success)
                {
                    // Load into currently running assembly and locate the new script.
                    var assy = Assembly.Load(ms.ToArray());
                    var types = assy.GetTypes();

                    foreach (Type t in types)
                    {
                        if (t is not null && t.Name == _scriptName)
                        {
                            // We have a good script file. Create the executable object.
                            script = Activator.CreateInstance(t);
                        }
                    }

                    if (script is null)
                    {
                        RecordReport(ReportType.Syntax, $"Couldn't find type {_scriptName}.");
                    }
                }

                // Collect results.
                foreach (var diag in result.Diagnostics)
                {
                    // Get the original context.
                    var fileName = diag.Location.SourceTree!.FilePath;
                    var lineNum = diag.Location.GetLineSpan().StartLinePosition.Line; // 0-based
                    var msg = diag.GetMessage();

                    var sfiles = _scriptFiles.Where(f => f.GeneratedFileName == fileName);
                    if (sfiles.Any()) // It's a script file.
                    {
                        var sf = sfiles.First();
                        RecordReport(ReportType.Syntax, msg, sf.SourceFileName, sf.GetSourceLineNumber(lineNum));
                    }
                    else if (_plainFiles.Contains(fileName))
                    {
                        RecordReport(ReportType.Syntax, msg, fileName, lineNum + 1);
                    }
                    else // other error?
                    {
                        RecordReport(ReportType.Error, msg, fileName, lineNum + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                RecordReport(ReportType.Error, $"Compiler exception: {ex}");
            }

            return script;
        }

        /// <summary>Parse one file. Recursive to support nested Include(fn).</summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool PreprocessFile(ScriptFile pcont)
        {
            bool valid = File.Exists(pcont.SourceFileName);

            if (valid)
            {
                pcont.GeneratedFileName = $"{_scriptName}_src{_scriptFiles.Count}.cs".ToLower();
                _scriptFiles.Add(pcont);

                // Preamble.
                pcont.GeneratedCode.AddRange(GenTopOfFile(pcont.SourceFileName));

                // The content.
                List<string> sourceLines = [.. File.ReadAllLines(pcont.SourceFileName)];

                for (int sourceLineNumber = 0; sourceLineNumber < sourceLines.Count; sourceLineNumber++)
                {
                    string s = sourceLines[sourceLineNumber];

                    // Remove any comments. Single line type only.
                    int pos = s.IndexOf("//");
                    string cline = pos >= 0 ? s.Left(pos) : s;

                    // Test for preprocessor directives.
                    string strim = s.Trim();

                    // like Include(path\utils.neb);
                    if (strim.StartsWith("Include"))
                    {
                        // Exclude from output file.
                        List<string> parts = strim.SplitByTokens("()");
                        if (parts.Count >= 2)
                        {
                            string fn = parts[1].Replace("\"", "");
                            
                            // Check for default path.
                            if(!File.Exists(fn))
                            {
                                if (ScriptPath != "")
                                {
                                    fn = Path.Combine(ScriptPath, fn);
                                }
                            }

                            // Recursive call to parse this file
                            ScriptFile subcont = new(fn);
                            valid = PreprocessFile(subcont);
                        }
                        else
                        {
                            valid = false;
                        }

                        if (!valid)
                        {
                            RecordReport(ReportType.Syntax, $"Invalid Include: {strim}", pcont.SourceFileName, sourceLineNumber);
                        }
                    }
                    else if (PreprocessLine(strim, pcont))
                    {
                       // NOP
                    }
                    else // plain line
                    {
                        if (cline.Trim() != "")
                        {
                            // Store the whole line.
                            pcont.LineNumberMap[pcont.GeneratedCode.Count] = sourceLineNumber + 1;
                            pcont.GeneratedCode.Add(_addWrapper ? $"        {cline}" : $"    {cline}");
                        }
                    }
                }

                // Postamble.
                pcont.GeneratedCode.AddRange(GenBottomOfFile());
            }

            return valid;
        }

        /// <summary>
        /// Create the boilerplate file top stuff.
        /// </summary>
        /// <param name="fn">Source file name. Empty means it's an internal file.</param>
        /// <returns></returns>
        List<string> GenTopOfFile(string fn)
        {
            string origin = fn == "" ? "internal" : fn;

            // Create the common contents.
            List<string> codeLines =
            [
                $"// Created from {origin} {DateTime.Now}",
            ];

            SystemDlls.ForEach(d => codeLines.Add($"using {d};"));
            LocalDlls.ForEach(d => codeLines.Add($"using {d};"));
            Usings.ForEach(d => codeLines.Add($"using {d};"));

            codeLines.AddRange(
            [
                "",
                $"namespace {_scriptName}.UserScript",
                "{",
            ]);

            if(_addWrapper)
            {
                codeLines.AddRange(
                [
                   $"    public partial class {_scriptName} : {_apiName}",
                    "    {"
                ]);
            }

            return codeLines;
        }

        /// <summary>
        /// Create the boilerplate file bottom stuff.
        /// </summary>
        /// <returns></returns>
        List<string> GenBottomOfFile()
        {
            // Create the common contents.
            List<string> codeLines = _addWrapper ? new() { "    }", "}" } : new() { "}" };

            return codeLines;
        }

        /// <summary>Internal to our library codes.</summary>
        /// <param name="severity"></param>
        /// <returns></returns>
        ReportType Translate(DiagnosticSeverity severity)//, bool ignoreWarnings)
        {
            var resType = severity switch
            {
                DiagnosticSeverity.Hidden => ReportType.Error, //Other,
                DiagnosticSeverity.Info => ReportType.Info,
                DiagnosticSeverity.Warning => ReportType.Warning,
                DiagnosticSeverity.Error => ReportType.Error,
                _ => ReportType.None
            };

            return resType == ReportType.Warning && IgnoreWarnings ? ReportType.None : resType;
        }

        protected void RecordReport(ReportType resultType, string msg, string? scriptFile = null, int? lineNum = null)
        {
            if (scriptFile is not null)
            {
                scriptFile = Path.GetFileName(scriptFile);
            }
            var res = new Report() //resultType, msg);
            {
                ReportType = resultType,
                Message = msg,
                SourceFileName = scriptFile,
                SourceLineNumber = lineNum ?? -1
            };
            Reports.Add(res);
        }
        #endregion
    }
}
