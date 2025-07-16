using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Ephemera.NBagOfTricks;


// TODOX does this apply? https://carljohansen.wordpress.com/2020/05/09/compiling-expression-trees-with-roslyn-without-memory-leaks-2/
// Not for the Example (run to completion then exit) but yes for long-running apps that reload externally modified scripts.
// Assemblies, once loaded, cannot be unloaded until the process shuts down or the AssemblyLoadContext is unloaded.
// In  .NET Framework there's no way to unload, but in .NET Core you can use an alternate AssemblyLoadContext which if provided
// can be used to unload assemblies loaded in the context conditionally.



namespace Ephemera.NScript
{
    /// <summary>Parses/compiles script file(s).</summary>
    public class CompilerCore
    {
        #region Properties
        /// <summary>Client option.</summary>
        public bool IgnoreWarnings { get; set; } = false;

        ///// <summary>Client may need to tell us this for #:include directive.</summary>
        //public string? ScriptPath { get; set; }

        /// <summary>Script namespace.</summary>
        public string Namespace { get; set; } = "Anonymous";

        /// <summary>Base class.</summary>
        public string BaseClassName { get; set; } = "GenericClass";

        /// <summary>Client may weant to tell us to use this for temp files.</summary>
        public string? TempDir { get; set; }

        /// <summary>Default system dlls. Client can add or subtract.</summary>
        public List<string> SystemDlls { get; set; } = [];
        //[
        //    "System",
        //    "System.Private.CoreLib",
        //    "System.Runtime",
        //    "System.IO",    
        //    "System.Collections",
        //    "System.Linq"
        //];

        /// <summary>Additional using statements not supplied by core dlls.</summary>
        public List<string> Usings { get; set; } = [];
        //[
        //    "System.Collections.Generic",
        //    "System.Diagnostics",
        //    "System.Text"
        //];

        /// <summary>App dlls supplied by app compiler.</summary>
        public List<string> LocalDlls { get; set; } = [];

        /// <summary>The final compiled script.</summary>
        public object? CompiledScript { get; set; } = null;

        /// <summary>Accumulated errors and other bits of information - for user presentation.</summary>
        public List<Report> Reports { get; } = [];

        /// <summary>All the script directives - name and value. These are global, not per file!</summary>
        public List<(string dirname, string dirval)> Directives { get; } = [];

        /// <summary>All active script source files. Provided so client can monitor for external changes.</summary>
        public IEnumerable<string> SourceFiles { get { return [.. _scriptFiles.Select(f => f.SourceFileName)]; } }
        #endregion

        #region Fields
 //       /// <summary>Script info.</summary>
//        string _scriptName = "???";

        /// <summary>Products of preprocess.</summary>
        readonly List<ScriptFile> _scriptFiles = [];

        /// <summary>Other files to compile.</summary>
        readonly List<string> _plainFiles = [];
        #endregion

        #region Overrides for derived classes to hook
        /// <summary>Called before compiler starts.</summary>
        protected virtual void PreCompile() { }

        /// <summary>Called after compiler finished.</summary>
        protected virtual void PostCompile() { }

        /// <summary>Called for each line in the source file before compiling.</summary>
        /// <param name="sline">Trimmed line</param>
        /// <param name="lineNum">Source line number may be useful (1-based)</param>
        /// <param name="pcont">File context</param>
        /// <returns>True if derived class took care of this</returns>
        protected virtual bool PreprocessLine(string sline, int lineNum, ScriptFile pcont) { return false; }
        #endregion

        string MakeClassName(string fn)
        {
            // Get and sanitize the script name.
            var fff = Path.GetFileNameWithoutExtension(fn);
            StringBuilder sb = new();
            fff.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
            return sb.ToString();
        }

        #region Public functions
        /// <summary>Run the compiler on a script file.</summary>
        /// <param name="scriptFile">Path to main script file.</param>
        /// <param name="sourceFiles">Source code files.</param>
        public void CompileScript(string scriptFile, List<string>? sourceFiles = null)
        {
            // Reset everything.
            CompiledScript = null;
            Reports.Clear();
            Directives.Clear();

            _scriptFiles.Clear();
            _plainFiles.Clear();

            var scriptName = "???";

            try
            {
                DateTime startTime = DateTime.Now;

                if (sourceFiles != null)
                {
                    _plainFiles.AddRange(sourceFiles);
                }

                // Derived class hook.
                PreCompile();

                // Get and sanitize the script name.
                //_scriptName = Path.GetFileNameWithoutExtension(scriptFile);
                //StringBuilder sb = new();
                //_scriptName.ForEach(c => sb.Append(char.IsLetterOrDigit(c) ? c : '_'));
                //_scriptName = sb.ToString();
                scriptName = MakeClassName(scriptFile);

                var dir = Path.GetDirectoryName(scriptFile);
                if (dir is null)
                {
                    AddReport(ReportType.Syntax, ReportLevel.Error, $"Invalid script file: {scriptFile}");
                    throw new ScriptException();
                }

                // Create temp output area and/or clean it.
                var tempDir = TempDir ?? Path.Combine(dir, "temp");
                Directory.CreateDirectory(tempDir);
                Directory.GetFiles(tempDir).ForEach(f => File.Delete(f));

                // Process the source files into something that can be compiled.
                ScriptFile pcont = new(scriptFile) { TopLevel = true } ;
                bool valid = PreprocessFile(pcont); // >>> recursive function

                // Compile the processed files.
                //Compile(dir!);

                ///////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////


                CompiledScript = null;


                // Assemble constituents.
                List<SyntaxTree> trees = [];
                var encoding = Encoding.UTF8; // ASCII?

                // Write the generated source files to temp build area.
                foreach (var tocomp in _scriptFiles)
                {
                    if (tocomp.GeneratedCode.Count > 0)
                    {
                        // Create a file that can be placed in the pdb.
                        string fullpath = Path.Combine(tempDir, tocomp.GeneratedFileName);
                        File.WriteAllLines(fullpath, tocomp.GeneratedCode);
                        // Build a syntax tree.
                        string code = File.ReadAllText(fullpath, encoding);
                        CSharpParseOptions popts = new();
                        SyntaxTree tree = CSharpSyntaxTree.ParseText(text: code, path: fullpath, options: popts, encoding: encoding);
                        trees.Add(tree);
                    }
                }

                // Plain files require simpler handling.
                foreach (var fn in _plainFiles)
                {
                    // Build a syntax tree.
                    string code = File.ReadAllText(fn, encoding);
                    CSharpParseOptions popts = new();
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(text: code, path: fn, options: popts, encoding: encoding);
                    trees.Add(tree);
                }

                // References needed to compile the code.
                var references = new List<MetadataReference>();

                // System stuff location.
                var dotnetStore = Path.GetDirectoryName(typeof(object).Assembly.Location);

                // Project refs like nuget.
                var localStore = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                // System dlls.
                SystemDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll"))));
                // Had to add this for Enum per https://github.com/dotnet/roslyn/issues/50612
                references.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location));

                // Local dlls.
                LocalDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll"))));

                // Emit the whole mess to streams.
                using var ms = new MemoryStream();
                using var pdbs = new MemoryStream();

                var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
                var emitOptions = new EmitOptions().
                    WithDebugInformationFormat(DebugInformationFormat.PortablePdb).
                    WithDefaultSourceFileEncoding(encoding);
                var compilation = CSharpCompilation.Create($"{scriptName}", trees, references, copts);

                var result = compilation.Emit(peStream: ms, pdbStream: pdbs, options: emitOptions);

                if (result.Success)
                {
                    // Load into currently running assembly and locate the new script.
                    var assy = Assembly.Load(ms.ToArray(), pdbs.ToArray());
                    var types = assy.GetTypes();

                    foreach (Type t in types)
                    {
                        //AddReport(ReportType.Internal, ReportLevel.Info, $"Type {t.Name}.");
                        if (t is not null && t.Name == scriptName)
                        {
                            // We have a good script file. Create the executable object.
                            CompiledScript = Activator.CreateInstance(t);
                        }
                    }

                    if (CompiledScript is null)
                    {
                        AddReport(ReportType.Internal, ReportLevel.Error, $"Couldn't activate script {scriptName}.");
                        throw new ScriptException(); // fatal
                    }
                }

                // Collect results.
                bool fatal = false;
                foreach (var diag in result.Diagnostics)
                {
                    // Get the original context.
                    var fileName = diag.Location != Location.None ? diag.Location.SourceTree!.FilePath : "No File";
                    var lineNum = diag.Location != Location.None ? diag.Location.GetLineSpan().StartLinePosition.Line : -1; // 0-based
                    var msg = diag.GetMessage();
                    var level = Translate(diag.Severity);

                    var sfiles = _scriptFiles.Where(f => f.GeneratedFileName == Path.GetFileName(fileName));
                    if (sfiles.Any()) // It's a script file.
                    {
                        var sf = sfiles.First();
                        int srcLineNum = sf.GetSourceLineNumber(lineNum);

                        if (srcLineNum == -1) // something in user api or compiler, probably
                        {
                            AddReport(ReportType.Syntax, level, $"{msg} => {sf.GeneratedCode[lineNum]}", sf.SourceFileName, srcLineNum);
                        }
                        else // regular user error
                        {
                            AddReport(ReportType.Syntax, level, msg, sf.SourceFileName, srcLineNum);
                        }
                    }
                    else if (_plainFiles.Contains(fileName))
                    {
                        AddReport(ReportType.Syntax, level, msg, fileName, lineNum + 1);
                    }
                    else // other error?
                    {
                        AddReport(ReportType.Internal, ReportLevel.Error, msg, fileName, lineNum + 1);
                    }
                    fatal |= (level == ReportLevel.Error);
                }
                if (fatal)
                {
                    throw new ScriptException();
                }






                ///////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////



                AddReport(ReportType.Internal, ReportLevel.Info, $"Compiled script: {(DateTime.Now - startTime).Milliseconds} msec.");

                // Derived class hook.
                PostCompile();
            }
            catch (ScriptException)
            {
                // It's dead Jim. Content already reported.
                CompiledScript = null;
            }
            catch (Exception ex)
            {
                // Something else not detected in normal operation. Also dead.
                AddReport(ReportType.Internal, ReportLevel.Error, $"CompileScript other exception: {ex}.");
                CompiledScript = null;
            }
        }

        /// <summary>
        /// Run the compiler on a simple text block.
        /// </summary>
        /// <param name="text">Text to compile.</param>
        public Assembly? CompileText(string text)
        {
            DateTime startTime = DateTime.Now;

            // Build a syntax tree.
            CSharpParseOptions popts = new();
            SyntaxTree tree = CSharpSyntaxTree.ParseText(text, popts);

            // References needed to compile the code.
            var references = new List<MetadataReference>();
            var dotnetStore = Path.GetDirectoryName(typeof(object).Assembly.Location);
            SystemDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll"))));

            // Emit to stream.
            using var ms = new MemoryStream();
            var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create($"SimpleText.dll", [tree], references, copts);
            EmitResult result = compilation.Emit(ms);

            if (result.Success)
            {
                // Load into currently running assembly.
                var assy = Assembly.Load(ms.ToArray());
                AddReport(ReportType.Internal, ReportLevel.Info, $"Compiled text: {(DateTime.Now - startTime).Milliseconds} msec.");
                return assy;
            }
            else
            {
                // Collect results.
                foreach (var diag in result.Diagnostics)
                {
                    //var msg = diag.GetMessage();
                    var lineNum = diag.Location.GetLineSpan().StartLinePosition.Line + 1;
                    AddReport(ReportType.Internal, Translate(diag.Severity), $"Compiled text failed: {diag.GetMessage()}");
                }
            }
            return null;
        }

        /// <summary>
        /// Process script runtime exceptions.
        /// Technically not part of compiler but this has all the info needed to diagnose runtime errors.
        /// </summary>
        /// <param name="ex">The exception to examine.</param>
        public void ProcessRuntimeException(Exception ex)
        {
            // If there is an inner exception it was generated by the script - from the other side of Invoke();
            // Otherwise it was generated from this side.
            var exToExamine = ex.InnerException is null ? ex : ex.InnerException;

            // Get the file/line info. The first valid filename is the one of interst.
            var stackTrace = new StackTrace(exToExamine, true);
            foreach (var frame in stackTrace.GetFrames())
            {
                var fileName = frame.GetFileName();
                if (fileName is not null)
                {
                    var lineNum = frame.GetFileLineNumber();
                    var msg = exToExamine.Message;

                    var sfiles = _scriptFiles.Where(f => f.GeneratedFileName == Path.GetFileName(fileName));
                    if (sfiles.Any()) // It's a script file.
                    {
                        var sf = sfiles.First();
                        int srcLineNum = sf.GetSourceLineNumber(lineNum);

                        if (srcLineNum == -1) // something in user api or compiler, probably
                        {
                            AddReport(ReportType.Runtime, ReportLevel.Error, $"{msg} => {sf.GeneratedCode[lineNum - 1]}", sf.SourceFileName, srcLineNum);
                        }
                        else // regular user error
                        {
                            AddReport(ReportType.Runtime, ReportLevel.Error, msg, sf.SourceFileName, srcLineNum);
                        }
                    }
                    else if (_plainFiles.Contains(fileName))
                    {
                        AddReport(ReportType.Runtime, ReportLevel.Error, msg, fileName, lineNum);
                    }
                    else // probably user app
                    {
                        AddReport(ReportType.Runtime, ReportLevel.Error, msg, fileName, lineNum);
                    }
                    break;
                }
            }
        }
        #endregion

        #region Private and protected functions
        ///// <summary>The actual script compiler worker.</summary>
        ///// <param name="baseDir">Fully qualified path to main script file dir.</param>
        //void Compile(string baseDir)
        //{
        //    CompiledScript = null;

        //    // Create temp output area and/or clean it.
        //    var tempDir = TempDir ?? Path.Combine(baseDir, "temp");
        //    Directory.CreateDirectory(tempDir);
        //    Directory.GetFiles(tempDir).ForEach(f => File.Delete(f));

        //    // Assemble constituents.
        //    List<SyntaxTree> trees = [];
        //    var encoding = Encoding.UTF8; // ASCII?

        //    // Write the generated source files to temp build area.
        //    foreach (var tocomp in _scriptFiles)
        //    {
        //        if (tocomp.GeneratedCode.Count > 0)
        //        {
        //            // Create a file that can be placed in the pdb.
        //            string fullpath = Path.Combine(tempDir, tocomp.GeneratedFileName);
        //            File.WriteAllLines(fullpath, tocomp.GeneratedCode);
        //            // Build a syntax tree.
        //            string code = File.ReadAllText(fullpath, encoding);
        //            CSharpParseOptions popts = new();
        //            SyntaxTree tree = CSharpSyntaxTree.ParseText(text: code, path: fullpath, options: popts, encoding: encoding);
        //            trees.Add(tree);
        //        }
        //    }

        //    // Plain files require simpler handling.
        //    foreach (var fn in _plainFiles)
        //    {
        //        // Build a syntax tree.
        //        string code = File.ReadAllText(fn, encoding);
        //        CSharpParseOptions popts = new();
        //        SyntaxTree tree = CSharpSyntaxTree.ParseText(text: code, path: fn, options: popts, encoding: encoding);
        //        trees.Add(tree);
        //    }

        //    // References needed to compile the code.
        //    var references = new List<MetadataReference>();

        //    // System stuff location.
        //    var dotnetStore = Path.GetDirectoryName(typeof(object).Assembly.Location);

        //    // Project refs like nuget.
        //    var localStore = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        //    // System dlls.
        //    SystemDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(dotnetStore!, dll + ".dll"))));
        //    // Had to add this for Enum per https://github.com/dotnet/roslyn/issues/50612
        //    references.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location));

        //    // Local dlls.
        //    LocalDlls.ForEach(dll => references.Add(MetadataReference.CreateFromFile(Path.Combine(localStore!, dll + ".dll"))));

        //    // Emit the whole mess to streams.
        //    using var ms = new MemoryStream();
        //    using var pdbs = new MemoryStream();

        //    var copts = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        //    var emitOptions = new EmitOptions().
        //        WithDebugInformationFormat(DebugInformationFormat.PortablePdb).
        //        WithDefaultSourceFileEncoding(encoding);
        //    var compilation = CSharpCompilation.Create($"{_scriptName}", trees, references, copts);

        //    var result = compilation.Emit(peStream: ms, pdbStream: pdbs, options: emitOptions);

        //    if (result.Success)
        //    {
        //        // Load into currently running assembly and locate the new script.
        //        var assy = Assembly.Load(ms.ToArray(), pdbs.ToArray());
        //        var types = assy.GetTypes();

        //        foreach (Type t in types)
        //        {
        //            AddReport(ReportType.Internal, ReportLevel.Info, $"Type {t.Name}.");
        //            if (t is not null && t.Name == _scriptName)
        //            {
        //                // We have a good script file. Create the executable object.
        //                CompiledScript = Activator.CreateInstance(t);
        //            }
        //        }

        //        if (CompiledScript is null)
        //        {
        //            AddReport(ReportType.Internal, ReportLevel.Error, $"Couldn't activate script {_scriptName}.");
        //            throw new ScriptException(); // fatal
        //        }
        //    }

        //    // Collect results.
        //    bool fatal = false;
        //    foreach (var diag in result.Diagnostics)
        //    {
        //        // Get the original context.
        //        var fileName = diag.Location != Location.None ? diag.Location.SourceTree!.FilePath : "No File";
        //        var lineNum = diag.Location != Location.None ? diag.Location.GetLineSpan().StartLinePosition.Line : -1; // 0-based
        //        var msg = diag.GetMessage();
        //        var level = Translate(diag.Severity);

        //        var sfiles = _scriptFiles.Where(f => f.GeneratedFileName == Path.GetFileName(fileName));
        //        if (sfiles.Any()) // It's a script file.
        //        {
        //            var sf = sfiles.First();
        //            int srcLineNum = sf.GetSourceLineNumber(lineNum);

        //            if (srcLineNum == -1) // something in user api or compiler, probably
        //            {
        //                AddReport(ReportType.Syntax, level, $"{msg} => {sf.GeneratedCode[lineNum]}", sf.SourceFileName, srcLineNum);
        //            }
        //            else // regular user error
        //            {
        //                AddReport(ReportType.Syntax, level, msg, sf.SourceFileName, srcLineNum);
        //            }
        //        }
        //        else if (_plainFiles.Contains(fileName))
        //        {
        //            AddReport(ReportType.Syntax, level, msg, fileName, lineNum + 1);
        //        }
        //        else // other error?
        //        {
        //            AddReport(ReportType.Internal, ReportLevel.Error, msg, fileName, lineNum + 1);
        //        }
        //        fatal |= (level == ReportLevel.Error);
        //    }
        //    if (fatal)
        //    {
        //        throw new ScriptException();
        //    }
        //}

        /// <summary>Parse one file. Recursive to support nested #:include fn.</summary>
        /// <param name="pcont">The parse context.</param>
        /// <returns>True if a valid file.</returns>
        bool PreprocessFile(ScriptFile pcont)
        {
            bool valid = File.Exists(pcont.SourceFileName);

            if (valid)
            {
                pcont.GeneratedFileName = $"{Path.GetFileNameWithoutExtension(pcont.SourceFileName)}_generated.cs";
                // pcont.GeneratedFileName = $"{_scriptName}_src{_scriptFiles.Count}.cs".ToLower();
                _scriptFiles.Add(pcont);

                // Preamble.
                pcont.GeneratedCode.AddRange(GenTopOfFile(pcont));

                // The content.
                List<string> sourceLines = [.. File.ReadAllLines(pcont.SourceFileName)];

                for (int sourceLineNumber = 0; sourceLineNumber < sourceLines.Count && valid; sourceLineNumber++)
                {
                    string s = sourceLines[sourceLineNumber];

                    // Remove any comments. Single line type only.
                    int pos = s.IndexOf("//");
                    string cline = pos >= 0 ? s.Left(pos) : s;

                    // Tidy up.
                    string strim = s.Trim();

                    // Test for app preprocessor directives like #:include path\utils.neb.
                    if (strim.StartsWith("#:"))
                    {
                        strim = strim.Replace("#:", "");

                        if (strim.Length == 0)
                        {
                            valid = false;
                        }
                        else
                        {
                            int dpos = strim.IndexOf(' ');
                            var directive = dpos == -1 ? strim : strim.Left(dpos);
                            var dval = dpos == -1 ? "" : strim.Right(strim.Length - dpos - 1);

                            // Handle include now.
                            if (directive == "include")
                            {
                                var incfn = RationalizeFileName(pcont.SourceFileName, dval);

                                if (incfn is not null)
                                {
                                    // Recursive call to parse this file
                                    ScriptFile subcont = new(incfn);
                                    valid = PreprocessFile(subcont);
                                }
                                else
                                {
                                    AddReport(ReportType.Syntax, ReportLevel.Error, $"Invalid include ", pcont.SourceFileName, pcont.GetSourceLineNumber(sourceLineNumber));
                                    throw new ScriptException();
                                }
                            }
                            else
                            {
                                // Just add to global collection.
                                Directives.Add((directive, dval));
                            }
                        }

                        if (!valid)
                        {
                            AddReport(ReportType.Syntax, ReportLevel.Error, $"Invalid directive: {strim}", pcont.SourceFileName, sourceLineNumber + 1);
                            throw new ScriptException(); // fatal
                        }
                    }
                    else if (PreprocessLine(strim, sourceLineNumber + 1, pcont))
                    {
                       // handled = NOP
                    }
                    else // plain line
                    {
                        if (cline.Trim() != "")
                        {
                            // Store the whole line.
                            pcont.LineNumberMap[pcont.GeneratedCode.Count] = sourceLineNumber + 1;
                            pcont.GeneratedCode.Add($"        {cline}");
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
        /// <param name="pcont">Source file info.</param>
        /// <returns></returns>
        List<string> GenTopOfFile(ScriptFile pcont)
        {
            string fn = pcont.SourceFileName;
            string origin = fn == "" ? "internal" : fn; // .\Game999.csx
            string className = MakeClassName(origin);

            string baseClass = pcont.TopLevel ? $" : {BaseClassName}" : "";

            // Create the common contents. Like  public class Game999 : ScriptCore
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
                $"namespace {Namespace}",
                 "{",
                $"    public class {className}{baseClass}",
                 "    {",
            ]);

            return codeLines;
        }

        /// <summary>
        /// Create the boilerplate file bottom stuff.
        /// </summary>
        /// <returns></returns>
        List<string> GenBottomOfFile()
        {
            // Create the common contents.
            List<string> codeLines =
            [
                "    }",
                "}"
            ];

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

        /// <summary>Capture script errors.</summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        /// <param name="scriptFile"></param>
        /// <param name="lineNum"></param>
        protected void AddReport(ReportType type, ReportLevel level, string msg, string? scriptFile = null, int? lineNum = null)
        {
            if (level != ReportLevel.None)
            {
                if (scriptFile is not null)
                {
                    scriptFile = Path.GetFileName(scriptFile);
                }

                var rep = new Report()
                {
                    ReportType = type,
                    Level = level,
                    Message = msg,
                    SourceFileName = scriptFile,
                    SourceLineNumber = lineNum ?? -1
                };
                Reports.Add(rep);
            }
        }

        /// <summary>
        /// Determines absolute file name for an included file.
        /// </summary>
        /// <param name="scriptFileName">File with include directive</param>
        /// <param name="includeFileName">Included file.</param>
        /// <returns></returns>
        string? RationalizeFileName(string scriptFileName, string includeFileName)
        {
            string? fn;

            if (Path.IsPathFullyQualified(includeFileName)) // Explicit?
            {
                fn = includeFileName;
            }
            else // relative
            {
                var dir = Path.GetDirectoryName(scriptFileName);
                fn = dir is null ? null : Path.Combine(dir, includeFileName);
            }

            // Check for file existence.
            if (fn is not null)
            {
                fn = File.Exists(fn) ? fn : null;
            }
            return fn;
        }
        #endregion
    }
}
