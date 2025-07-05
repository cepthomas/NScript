using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Ephemera.NBagOfTricks;


namespace NScript.Example
{
    class Example
    {
        /// <summary>
        /// Run script compiler using example file.
        /// This demonstrates how a host application loads and runs scripts.
        /// </summary>
        public int Run()
        {
            int retcode = 0;

            ///// Compile script with your options.
            GameCompiler compiler = new()
            {
                ScriptPath = MiscUtils.GetSourcePath(),
                IgnoreWarnings = false,
            };

            var scriptFile = Path.Combine(compiler.ScriptPath, "Game999.csx");
            var baseFile = Path.Combine(compiler.ScriptPath, "GameScriptBase.cs");
            //var baseFile = Path.Combine(compiler.ScriptPath, "..", "Script", "GameScriptBase.cs");
            compiler.CompileScript(scriptFile, baseFile);

            if (compiler.CompiledScript is null)
            {
                // It failed.
                Console.WriteLine($"Compile Failed:");
                compiler.Reports.ForEach(rep => Console.WriteLine($"{rep}"));
                return 1;
            }

            ///// Execute script. Needs exception handling to protect from user runtime script errors.
            try
            {
                // Init script.
                var script = compiler.CompiledScript;
                var scriptType = script.GetType();

                // TODO1 improve on this?
                //https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/#reflection
                // Supposedly delegate is created under the cover so we don't have to write it.
                //If you know at compile - time the signature of the target method use MethodInfo.CreateDelegate
                //https://learn.microsoft.com/dotnet/api/system.reflection.methodinfo.createdelegate

                var methodInit = scriptType.GetMethod("Init");
                var methodSetup = scriptType.GetMethod("Setup");
                var methodMove = scriptType.GetMethod("Move");

                methodInit.Invoke(script, [Console.Out]);
                methodSetup.Invoke(script, ["Here I am!!!", "too many"]);

                for (int i = 0; i < 10; i++)
                { 
                    methodMove.Invoke(script, []);  
                }
            }
            catch (Exception ex) //TODO1 handle like compiler errors. By Core?
            {
                // runtime errors
                Console.WriteLine($"Runtime Exception: {ex}");
                Console.WriteLine($"Inner Exception: {ex.InnerException}");
                //Runtime Exception: System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
                // ---> System.ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')
                //   at System.Collections.Generic.List`1.get_Item(Int32 index)
                //   at NScript.Example.GameScriptBase.RandomPlayer() in C:\Dev\Libs\NScript\Example\GameScriptBase.cs:line 74
                //   at UserScript.Utils.Boing(Int32 which) in C:\Dev\Libs\NScript\Example\temp\game999_src1.cs:line 27
                //   at UserScript.Game999.Setup(String info) in C:\Dev\Libs\NScript\Example\temp\game999_src0.cs:line 30
                //   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
                //   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
                //   --- End of inner exception stack trace ---
                //   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
                //   at System.Reflection.MethodBaseInvoker.InvokeWithOneArg(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
                //   at System.Reflection.MethodBase.Invoke(Object obj, Object[] parameters)
                //   at NScript.Example.Example.Run() in C:\Dev\Libs\NScript\Example\Example.cs:line 59

                var stackTrace = new StackTrace(ex.InnerException, true);
                foreach (var frame in stackTrace.GetFrames())
                {
                    var fn = frame.GetFileName();
                    var ln = frame.GetFileLineNumber();
                    var method = frame.GetMethod();
                    Console.WriteLine($"fn:{fn} ln:{ln} method:{method}");
                }


                // or syntax errors like this:
                //Runtime Exception: System.Reflection.TargetParameterCountException: Parameter count mismatch.
                //   at System.Reflection.MethodBaseInvoker.ThrowTargetParameterCountException()
                //   at System.Reflection.RuntimeMethodInfo.Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
                //   at System.Reflection.MethodBase.Invoke(Object obj, Object[] parameters)
                //   at NScript.Example.Example.Run() in C:\Dev\Libs\NScript\Example\Example.cs:line 59
                //Inner Exception: ???
                //Unhandled exception. System.ArgumentNullException: Value cannot be null. (Parameter 'e')
                //   at System.ArgumentNullException.Throw(String paramName)
                //   at System.Diagnostics.StackTrace..ctor(Exception e, Boolean fNeedFileInfo)
                //   at NScript.Example.Example.Run() in C:\Dev\Libs\NScript\Example\Example.cs:line 85
                //   at NScript.Example.Program.Main(String[] args) in C:\Dev\Libs\NScript\Example\Example.cs:line 178
            }

            return retcode;
        }
    }

    /// <summary>Test compiler.</summary>
    class GameCompiler : CompilerCore
    {
        /// <summary>Info collected from the script.</summary>
        bool _gotBurger = false;

        /// <summary>Called before compiler starts.</summary>
        public override void PreCompile()
        {
            LocalDlls = ["NScript"];
            //LocalDlls = ["Ephemera.NBagOfTricks", "NScript"];
            //SystemDlls.Add("System");
            Usings.Add("NScript.Example");
        }

        /// <summary>Called after compiler finished.</summary>
        public override void PostCompile()
        {
            if (_gotBurger)
            {
                ReportInternal(ReportLevel.Info, "Script has a cheeseburger!");
            }
        }

        /// <summary>Called for each line in the source file before compiling.</summary>
        /// <param name="sline"></param>
        /// <param name="pcont"></param>
        /// <returns>True - exclude from output file</returns>
        public override bool PreprocessLine(string sline, ScriptFile pcont)
        {
            bool handled = false;

            // Check for my specials.
            string strim = sline.Trim();

            if (strim.StartsWith("KustomDirective"))
            {
                bool valid = false;
                handled = true;

                List<string> parts = strim.SplitByTokens("()");
                if (parts.Count == 2)
                {
                    string val = parts[1].Replace("\"", "");
                    if (val == "cheeseburger")
                    {
                        _gotBurger = true;
                        valid = true;
                    }
                }

                if (!valid)
                {
                    ReportSyntax(ReportLevel.Error, $"Invalid KustomDirective: {strim}", pcont.SourceFileName, -99999999 );
                }
            }

            return handled;
        }
    }

    /// <summary>Start here.</summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            Environment.Exit(new Example().Run());
        }
    }
}
