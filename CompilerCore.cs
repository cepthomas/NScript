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
    /// <summary>General script result - error/warn etc.</summary>
    public enum CompileResultType
    {
        None,       // Ignore.
        Info,       // Not an error.
        Warning,    // Compiler warning.
        Error,      // Compiler error.
        //Other       // Custom use.
    }

    /// <summary>General script result container.</summary>
    /// <remarks>Convenience constructor.</remarks>
    public class CompileResult(CompileResultType resultType, string msg)
    {
        /// <summary>Where it came from.</summary>
        public CompileResultType ResultType { get; set; } = resultType;

        /// <summary>Original source file.</summary>
        public string? SourceFileName { get; set; }

        /// <summary>Original source line number 1-based. -1 means inapplicable or unknown.</summary>
        public int SourceLineNumber { get; set; } = -1;

        /// <summary>Content.</summary>
        public string Message { get; set; } = msg;

        /// <summary>For humans.</summary>
        public override string ToString()
        {
            StringBuilder sb = new($"{ResultType}: ");
            if (SourceFileName is not null)
            {
                sb.Append($"{SourceFileName}({SourceLineNumber}) ");
            }
            sb.Append($"[{Message}]");
            return sb.ToString();
        }
    }

    /// <summary>Parser file context class - one per original source file.</summary>
//    public class FileContext(string fn)
    public class ScriptFile(string fn)
    {
        /// <summary>Original source file.</summary>
        /*public*/
        public string SourceFileName { get; init; } = fn;

        ///// <summary>Current source line 0-based.</summary>
        //public int SourceLineNumber { get; set; } = 0;

        /// <summary>If not the same as SourceFileName this is a generated file.</summary>
        public string GeneratedFileNameX { get; private set; }

        /// <summary>If not null this is a generated file.</summary>
        //public string? GeneratedFileNameX { get; set; }
        //public string GeneratedFileName { get { return _genFileName ?? SourceFileName; } set { _genFileName = value; } }
        //string? _genFileName = null;

        /// <summary>Is CompileFileName generated or original?</summary>
//        public bool IsGenerated { get { return SourceFileName != CompiledFileName; } }

        /// <summary>The translated script code lines to feed the compiler.</summary>
        public List<string> GeneratedCode { get; set; } = [];

        /// <summary>key is GeneratedCode line number aka index, value is Source line number.</summary>
        public Dictionary<int, int> LineNumberMap { get; set; } = [];

        public (string sourceFileName, int sourceLineNum) GetSourceInfo(int lineNum)
        {
            //int ln = GeneratedFileNameX is null ? lineNum : LineNumberMap.TryGetValue(lineNum, out int value) ? value : -1;
            int ln = LineNumberMap.TryGetValue(lineNum, out int value) ? value : -1;
            return (SourceFileName, ln);
        }
    }
    #endregion

    /// <summary>Parses/compiles script file(s).</summary>
    public class CompilerCore
    {
        #region Properties
        /// <summary>Client option.</summary>
        public bool IgnoreWarnings { get; set; } = true;

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

        /// <summary>Accumulated errors and other results.</summary>
        public List<CompileResult> Results { get; } = [];

        /// <summary>All active script source files. Provided so client can monitor for external changes. TODO1 used?</summary>
        public IEnumerable<string> SourceFiles { get { return [.. _generatedFiles.Select(f => f.SourceFileName)]; } }
//        public IEnumerable<string> SourceFiles { get { return [.. _filesToCompile.Values.Select(f => f.OriginalSourceFileNameX)]; } }

        /// <summary>Compile products are here.</summary>
        public string TempDir { get; set; } = "???";
        #endregion

        #region Fields
        /// <summary>Script info.</summary>
        string _scriptName = "???";

        /// <summary>Script api.</summary>
        string _apiName = "???";

        ///// <summary>Accumulated lines to go in the constructor.</summary>
        //        readonly List<string> _initLines = [];

        ///// <summary>Products of file preprocess. Key is generated file name.</summary>
        //       readonly Dictionary<string, FileContext> _filesToCompile = [];
        /// <summary>Products of file preprocess.</summary>
        readonly List<ScriptFile> _scriptFiles = [];

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
        /// <summary>
        /// Run the compiler on a script file.
        /// </summary>
        /// <param name="scriptfn">Fully qualified path to main file.</param>
        /// <param name="apifn">Fully qualified path to api file.</param>
        public void CompileScript(string scriptfn, string apifn) // TODO1 combine with CompileText()?
        {
            // Reset everything.
            CompiledScript = null;
            Results.Clear();
            _scriptFiles.Clear();
            _plainFiles.Clear();
            //_initLines.Clear();

            try
            {
                DateTime startTime = DateTime.Now;

                // Add the api file.
                _plainFiles.Add(apifn);
                _apiName = Path.GetFileNameWithoutExtension(apifn);

                PreCompile();

                //Results.Add(new CompileResult()
                //{
                //    ResultType = CompileResultType.Info,
                //    Message = $"Compiling {scriptfn}."
                //});

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
                CompiledScript = CompileOne(dir!);

                Results.Add(new CompileResult(CompileResultType.Info, $"Compile script: {(DateTime.Now - startTime).Milliseconds} msec."));

                PostCompile();
            }
            catch (Exception ex)
            {
                Results.Add(new CompileResult(CompileResultType.Error, $"Compile exception: {ex}."));
            }


            //if (File.Exists(apifn))
            //{

            //}
            //else
            //{
            //    Results.Add(new CompileResult()
            //    {
            //        ResultType = CompileResultType.Error,
            //        Message = $"Invalid api file {apifn}."
            //    });
            //}

            //if (File.Exists(scriptfn))
            //{
            //    PreCompile();

            //    Results.Add(new CompileResult()
            //    {
            //        ResultType = CompileResultType.Info,
            //        Message = $"Compiling {scriptfn}."
            //    });

            //    ///// Get and sanitize the script name.
            //    _scriptName = Path.GetFileNameWithoutExtension(scriptfn);
            //    StringBuilder sb = new();
            //    _scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
            //    _scriptName = sb.ToString();
            //    var dir = Path.GetDirectoryName(scriptfn);

            //    ///// Compile.
            //    DateTime startTime = DateTime.Now; // for metrics

            //    ///// Process the source files into something that can be compiled. PreprocessFile is a recursive function.
            //    FileContext pcont = new(scriptfn);
            //    PreprocessFile(pcont);

            //    ///// Compile the processed files.
            //    CompiledScript = CompileDir(dir!);

            //    Results.Add(new CompileResult()
            //    {
            //        ResultType = CompileResultType.Info,
            //        Message = $"Compile script took {(DateTime.Now - startTime).Milliseconds} msec."
            //    });

            //    PostCompile();
            //}
            //else
            //{
            //    Results.Add(new CompileResult()
            //    {
            //        ResultType = CompileResultType.Error,
            //        Message = $"Invalid file {scriptfn}."
            //    });
            //}
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

            // System dlls. { "System", "System.Private.CoreLib", "System.Runtime", "System.Collections", "System.Linq" };
            SystemDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll"))));

            // Local dlls. none default.
            LocalDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll"))));

            // Emit to stream.
            var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create($"{_scriptName}.dll", trees, references, copts);
            var ms = new MemoryStream();
            EmitResult result = compilation.Emit(ms);

            Results.Add(new(CompileResultType.Info, $"CompilerCore: text took {(DateTime.Now - startTime).Milliseconds} msec."));

            if (result.Success)
            {
                // Load into currently running assembly.
                var assy = Assembly.Load(ms.ToArray());
                var types = assy.GetTypes();
                //>>> foreach (Type t in types)
            }

            // Collect results.
            foreach (var diag in result.Diagnostics)
            {
                //var msg = diag.GetMessage();
                //var lineNum = diag.Location.GetLineSpan().StartLinePosition.Line + 1;
                //var resType = Translate(diag.Severity, false);
                Results.Add(new(Translate(diag.Severity, false), diag.GetMessage())
                {
                    SourceLineNumber = diag.Location.GetLineSpan().StartLinePosition.Line + 1
                });
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// The actual compiler driver.
        /// </summary>
        /// <param name="baseDir">Fully qualified path to main file.</param>
        /// <returns>Compiled script</returns>
        object? CompileOne(string baseDir)
        {
            object? script = null;

            try // many ways to go wrong...
            {
                // Create temp output area and/or clean it.
                TempDir = Path.Combine(baseDir, "temp");
                Directory.CreateDirectory(TempDir);
                //var fff = Directory.GetFiles(TempDir);
                Directory.GetFiles(TempDir).ForEach(f => File.Delete(f));

                // Assemble constituents.
                List<SyntaxTree> trees = [];

                // Write the generated source files to temp build area.
                foreach (var tocomp in _scriptFiles)//.Keys)
                {
                    if (tocomp.GeneratedCode.Count > 0)
                    {
                        //FileContext ci = _filesToCompile[genFn];
                        string fullpath = Path.Combine(TempDir, tocomp.GeneratedFileNameX);
                        //File.Delete(fullpath);
                        File.WriteAllLines(fullpath, tocomp.GeneratedCode);

                        // Build a syntax tree.
                        string code = File.ReadAllText(fullpath);
                        CSharpParseOptions popts = new();
                        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, popts, tocomp.GeneratedFileNameX);
                        trees.Add(tree);
                    }
                    /*
                            public static SyntaxTree ParseText(
            string text,
            CSharpParseOptions? options = null,
            string path = "",
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)




                    */

                }

                foreach (var fn in _plainFiles)
                {
                    // Build a syntax tree.
                    string code = File.ReadAllText(fn);
                    CSharpParseOptions popts = new();
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(code, popts, fn);
                    trees.Add(tree);
                }

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

                ///// Emit to stream.
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
                            object? o = Activator.CreateInstance(t);
                            if(o is not null)
                            {
                                script = o;
                            }
                        }
                    }

                    if (script is null)
                    {
                        Results.Add(new CompileResult(CompileResultType.Error, $"Couldn't locate a class named {_scriptName}."));
                    }
                }

                ///// Collect results.
                foreach (var diag in result.Diagnostics)
                {
                    CompileResult se = new(Translate(diag.Severity, IgnoreWarnings), diag.GetMessage());

var fileName = diag.Location.SourceTree!.FilePath;
var lineNum = diag.Location.GetLineSpan().StartLinePosition.Line; // 0-based


                    // Get the original context.
                    //var info = pc

                    var context = _scriptFiles.Where(f => f.GeneratedFileNameX == fileName);

                    if (context.Any())
                    {
                        se.SourceFileName = context.First().SourceFileName;
                        // Dig out the original line number.
                        string origLine = context.First().GeneratedCode[lineNum];
                        int ind = origLine.LastIndexOf("//");
                        if (ind != -1)
                        {
                            se.SourceLineNumber = int.TryParse(origLine[(ind + 2)..], out int origLineNum) ? origLineNum : -1; // 1-based
                        }
                    }
                    //else presumably internal generated file - should never have errors.



                    //if (_filesToCompile.TryGetValue(Path.GetFileName(genFileName), out var context))
                    //{
                    //    se.SourceFile = context.OriginalSourceFileNameX;
                    //    // Dig out the original line number.
                    //    string origLine = context.GenCodeLinesX[genLineNum];
                    //    int ind = origLine.LastIndexOf("//");
                    //    if (ind != -1)
                    //    {
                    //        se.LineNumber = int.TryParse(origLine[(ind + 2)..], out int origLineNum) ? origLineNum : -1; // 1-based
                    //    }
                    //}
                    //else
                    //{
                    //    // Presumably internal generated file - should never have errors.
                    //    se.SourceFile = "";
                    //}
                    //if (keep)
                    //{
                    //    Results.Add(se);
                    //}

                    Results.Add(se);

                }
            }
            catch (Exception ex)
            {
                Results.Add(new CompileResult(CompileResultType.Error, $"Compile exception: {ex}"));
            }

            return script;
        }

        /// <summary>
        /// Parse one file. Recursive to support nested Include(fn).
        /// </summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool PreprocessFile(ScriptFile pcont)
        {
            bool valid = File.Exists(pcont.SourceFileName);

            if (valid)
            {
                pcont.GeneratedFileNameX = $"{_scriptName}_src{_scriptFiles.Count}.cs".ToLower();
                _filesToCompile.Add(pcont);

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
                            Results.Add(new CompileResult(CompileResultType.Error, $"Invalid Include: {strim}")
                            {
                                SourceFileName = pcont.SourceFileName,
                                SourceLineNumber = sourceLineNumber + 1
                            });
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
                            // Store the whole line with line number tacked on and some indentation. TODO1 or keep map(s) of source to genned line numbers?
                            //pcont.GeneratedCode.Add(_addWrapper ? $"        {cline} //{SourceLineNumber}" : $"    {cline} //{SourceLineNumber}");

                            // Store the whole line.
                            pcont.LineNumberMap[pcont.GeneratedCode.Count] = sourceLineNumber;
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
            //else
            //{
            //    codeLines.Add("// ===========> _addWrapper = false");
            //}

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
        /// <param name="ignoreWarnings"></param>
        /// <returns></returns>
        CompileResultType Translate(DiagnosticSeverity severity, bool ignoreWarnings)
        {
            var resType = severity switch
            {
                DiagnosticSeverity.Hidden => CompileResultType.Error, //Other,
                DiagnosticSeverity.Info => CompileResultType.Info,
                DiagnosticSeverity.Warning => CompileResultType.Warning,
                DiagnosticSeverity.Error => CompileResultType.Error,
                _ => CompileResultType.None
            };

            return resType == CompileResultType.Warning && IgnoreWarnings ? CompileResultType.None : resType;
        }
        #endregion
    }
}
