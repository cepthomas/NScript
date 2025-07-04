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
    public enum ReportType
    {
        Internal,   // Compiler error etc.
        Syntax,     // User script syntax error.
        Runtime,    // User script execution error.
    }

    public enum ReportLevel { None, Info, Warning, Error }

    /// <summary>General script result container.</summary>
    /// <remarks>Convenience constructor.</remarks>
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
            "System.Diagnostics",
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

        /// <summary>Script base/api.</summary>
        string _baseName = "???";

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
        /// <param name="basefn">Fully qualified path to api file.</param>
        public void CompileScript(string scriptfn, string basefn) // TODO1 combine with CompileText()?
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
                _plainFiles.Add(basefn);
                _baseName = Path.GetFileNameWithoutExtension(basefn);

                PreCompile();

                // Get/sanitize the script name.
                _scriptName = Path.GetFileNameWithoutExtension(scriptfn);
                StringBuilder sb = new();
                _scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
                _scriptName = sb.ToString();
                var dir = Path.GetDirectoryName(scriptfn);

                // Process the source files into something that can be compiled.
                ScriptFile pcont = new(scriptfn);
                PreprocessFile(pcont); // recursive function

                // Compile the processed files.
                Compile(dir!);

                ReportInternal(ReportLevel.Info, $"Compiled script: {(DateTime.Now - startTime).Milliseconds} msec.");

                PostCompile();
            }
            catch (Exception ex)
            {
                ReportInternal(ReportLevel.Error, $"Compile exception: {ex}.");
            }
        }

        /// <summary>
        /// Run the compiler on a simple text block.
        /// </summary>
        /// <param name="text">Text to compile.</param>
        public void CompileText(string text)
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

            // System dlls.
            SystemDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll"))));

            // Local dlls.
            LocalDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll"))));

            // Emit to stream.
            var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create($"{_scriptName}.dll", trees, references, copts);
            var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            ReportInternal(ReportLevel.Info, $"CompilerCore: text took {(DateTime.Now - startTime).Milliseconds} msec.");

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
 //               ReportInternal(Translate(diag.Severity), diag.GetMessage(), "TODO1 src file name", lineNum);
            }
        }
        #endregion

        #region Private functions
        /// <summary>The actual compiler driver.</summary>
        /// <param name="baseDir">Fully qualified path to main file.</param>
        ///// <returns>Compiled script</returns>
        void Compile(string baseDir)
        {
            CompiledScript = null;

            try // many ways to go wrong...
            {
                // Create temp output area and/or clean it.
                TempDir = Path.Combine(baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                // Assemble constituents.
                List<SyntaxTree> trees = [];

                // Write the generated source files to temp build area.
                foreach (var tocomp in _scriptFiles)
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

                // Plain files require simpler handling.
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

                //Console.WriteLine($"dotnetStore:{dotnetStore}\nlocalStore:{localStore}");
                //dotnetStore: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\8.0.17
                //localStore: C:\Dev\Libs\NScript\Example\bin\net8.0-windows\win-x64

                ////var tsys = MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, "System.dll"));
                //var sys_assy = Assembly.LoadFile(Path.Combine(dotnetStore!, "System.dll"));
                //var sys_types = sys_assy.GetTypes();
                //var sys_mods = sys_assy.GetModules();

                //var x_assy = Assembly.LoadFile(Path.Combine(localStore!, "Ephemera.NBagOfTricks.dll"));
                //var x_types = x_assy.GetTypes();
                //var x_mods = x_assy.GetModules();

                Console.WriteLine("hhhhhhhhhhhhhh");


                // System dlls.
                SystemDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll"))));

                // Local dlls.
                LocalDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll"))));

                // Emit to stream.
                var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

                var compilation = CSharpCompilation.Create($"{_scriptName}", trees, references, copts);

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
                            CompiledScript = Activator.CreateInstance(t);
                        }
                    }

                    if (CompiledScript is null)
                    {
                        ReportSyntax(ReportLevel.Error, $"Couldn't find type {_scriptName}.");
                    }
                }

                // Collect results.
                foreach (var diag in result.Diagnostics)
                {
                    // Get the original context.
                    var fileName = diag.Location != Location.None ? diag.Location.SourceTree!.FilePath : "No File";
                    var lineNum = diag.Location != Location.None ? diag.Location.GetLineSpan().StartLinePosition.Line : -1; // 0-based
                    var msg = diag.GetMessage();

                    var sfiles = _scriptFiles.Where(f => f.GeneratedFileName == fileName);
                    if (sfiles.Any()) // It's a script file.
                    {
                        var sf = sfiles.First();
                        int srcLineNum = sf.GetSourceLineNumber(lineNum);

                        if (srcLineNum == -1) // something in user api or compiler, probably
                        {
                            ReportSyntax(Translate(diag.Severity), $"{msg} => {sf.GeneratedCode[lineNum - 1]}", sf.SourceFileName, srcLineNum);
                        }
                        else // regular user error
                        {
                            ReportSyntax(Translate(diag.Severity), msg, sf.SourceFileName, srcLineNum);
                        }
                    }
                    else if (_plainFiles.Contains(fileName))
                    {
                        ReportSyntax(Translate(diag.Severity), msg, fileName, lineNum + 1);
                    }
                    else // other error? TODO1 feasible?
                    {
                        ReportSyntax(ReportLevel.Error, msg, fileName, lineNum + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportInternal(ReportLevel.Error, $"Compiler exception: {ex}");
            }
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
                            ReportSyntax(ReportLevel.Error, $"Invalid Include: {strim}", pcont.SourceFileName, sourceLineNumber);
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
                $"namespace UserScript",
                //$"namespace {_scriptName}.UserScript",
                "{",
            ]);

            if(_addWrapper)
            {
                codeLines.AddRange(
                [
                   $"    public partial class {_scriptName} : {_baseName}",
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
        ReportLevel Translate(DiagnosticSeverity severity)
        {
            var resType = severity switch
            {
                DiagnosticSeverity.Hidden => ReportLevel.Warning, //Other,
                DiagnosticSeverity.Info => ReportLevel.Info,
                DiagnosticSeverity.Warning => ReportLevel.Warning,
                DiagnosticSeverity.Error => ReportLevel.Error,
                _ => ReportLevel.None
            };

            return resType == ReportLevel.Warning && IgnoreWarnings ? ReportLevel.None : resType;
        }

        /// <summary>Compile errors, bad paths, etc.</summary>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        protected void ReportInternal(ReportLevel level, string msg)
        {
            if (level != ReportLevel.None)
            {
                var res = new Report()
                {
                    ReportType = ReportType.Internal,
                    Level = level,
                    Message = msg,
                };
                Reports.Add(res);
            }
        }

        /// <summary>Script/user syntax errors.</summary>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        /// <param name="scriptFile"></param>
        /// <param name="lineNum"></param>
        protected void ReportSyntax(ReportLevel level, string msg, string? scriptFile = null, int? lineNum = null)
        {
            if (level != ReportLevel.None)
            {
                if (scriptFile is not null)
                {
                    scriptFile = Path.GetFileName(scriptFile);
                }

                var res = new Report()
                {
                    ReportType = ReportType.Syntax,
                    Level = level,
                    Message = msg,
                    SourceFileName = scriptFile,
                    SourceLineNumber = lineNum ?? -1
                };
                Reports.Add(res);
            }
        }
        #endregion
    }
}
