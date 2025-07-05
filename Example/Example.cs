using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Ephemera.NBagOfTricks;


namespace NScript.Example
{
    internal class Example
    {
        /// <summary>
        /// Run script compiler using example file.
        /// This demonstrates how a host application loads and runs scripts.
        /// </summary>
        /// <param name="_">Args</param>
        static void Main(string[] _)
        {
            ///// Compile script.%
            GameCompiler compiler = new()
            {
                ScriptPath = MiscUtils.GetSourcePath(),
                IgnoreWarnings = false,
            };

            var scriptFile = Path.Combine(compiler.ScriptPath, "Game999.csx");
            //var baseFile = Path.Combine(compiler.ScriptPath, "GameScriptBase.cs");
            var baseFile = Path.Combine(compiler.ScriptPath, "..", "Script", "GameScriptBase.cs");
            compiler.CompileScript(scriptFile, baseFile);

            if (compiler.CompiledScript is null)
            {
                // It failed.
                Console.WriteLine($"Compile Failed:");
                compiler.Reports.ForEach(res => Console.WriteLine($"{res}"));
                Environment.Exit(1);
            }

            ///// Execute script. Needs exception handling to protect from user runtime script errors.
            try
            {
                // Init script.
                var script = compiler.CompiledScript;

                //var t = script.GetType();
                //while (t != null)
                //{
                //    Console.WriteLine($"{t} => {t.BaseType} {t.GetHashCode()}");
                //    foreach (var m in t.GetMethods())
                //    {
                //        Console.WriteLine($"{m}");
                //    }
                //    t = t.BaseType; 
                //}
                // ==>
                //UserScript.Game999 => NScript.Example.GameScriptBase
                //Void Nada()
                //Int32 Setup(System.String)
                //Int32 Move()
                //Int32 Setup(System.String)
                //Int32 Move()
                //System.Type GetType()
                //System.String ToString()
                //Boolean Equals(System.Object)
                //Int32 GetHashCode()
                //
                //NScript.Example.GameScriptBase => System.Object
                //Int32 Setup(System.String)
                //Int32 Move()
                //System.Type GetType()
                //System.String ToString()
                //Boolean Equals(System.Object)
                //Int32 GetHashCode()
                //
                //System.Object =>
                //System.Type GetType()
                //System.String ToString()
                //Boolean Equals(System.Object)
                //Boolean Equals(System.Object, System.Object)
                //Boolean ReferenceEquals(System.Object, System.Object)
                //Int32 GetHashCode()


                //// get the type Program from the assembly
                //Type programType = assembly.GetType("Program");
                //// Get the static Main() method info from the type
                //MethodInfo method = programType.GetMethod("Main");
                //// invoke Program.Main() static method
                //method.Invoke(null, null);

                // get the type Program from the assembly
                Type scriptType = script.GetType();
                // Get the static Main() method info from the type
                MethodInfo method = scriptType.GetMethod("Setup");
                // invoke Program.Main() static method
                method.Invoke(script, ["hooha"]);


                // // supposed to be like this:
                // //var script = compiler.CompiledScript as GameScriptBase;
                // var script = (NScript.Example.GameScriptBase)compiler.CompiledScript;
                // var res = script!.Setup("TODO1");
                // // Run the game loop.
                // for (int i = 0; i < 10; i++)
                // {
                //    script.Move();
                // }


                // // supposed to be like this:
                // //var script = compiler.CompiledScript as GameScriptBase;
                // var script = (NScript.Example.GameScriptBase)compiler.CompiledScript;
                // var res = script!.Setup("TODO1");
                // // Run the game loop.
                // for (int i = 0; i < 10; i++)
                // {
                //    script.Move();
                // }
            }
            catch (Exception ex) //TODO1 handle like compiler errors.
            {
                Console.WriteLine($"Runtime Exception: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException}");

                var stackTrace = new StackTrace(ex.InnerException, true);
                foreach (var frame in stackTrace.GetFrames())
                {
                    //Console.WriteLine($"{frame}");
                    bool bh = frame.HasSource();
                    var os = frame.GetNativeOffset();
                    var method = frame.GetMethod();
                    var mname = method.Name;
                    var vvv1 = method.DeclaringType.Name;
                    var vvv2 = method.DeclaringType.FullName;
                    var mmod = method.Module.Name;

                    var fn = frame.GetFileName();
                    var ln = frame.GetFileLineNumber();
                    Console.WriteLine($"fn:{fn} ln:{ln} method:{method}");
                }

                //Runtime Exception: Exception has been thrown by the target of an invocation.
                //Inner Exception: System.ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')
                //   at System.Collections.Generic.List`1.get_Item(Int32 index)
                //   at NScript.Example.GameScriptBase.RandomPlayer() in C:\Dev\Libs\NScript\Example\..\Script\GameScriptBase.cs:line 82
                //   at UserScript.Utils.Boing(Int32 which) in C:\Dev\Libs\NScript\Example\temp\game999_src1.cs:line 27
                //   at UserScript.Game999.Setup(String info) in C:\Dev\Libs\NScript\Example\temp\game999_src0.cs:line 33
                //   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
                //   at System.Reflection.MethodBaseInvoker.InvokeDirectByRefWithFewArgs(Object obj, Span`1 copyOfArgs, BindingFlags invokeAttr)
                //fn: ln:0 method:Void ThrowArgumentOutOfRange_IndexMustBeLessException()
                //fn: ln:0 method:T get_Item(Int32)
                //fn:C:\Dev\Libs\NScript\Example\..\Script\GameScriptBase.cs ln:82 method:System.String RandomPlayer()
                //fn:C:\Dev\Libs\NScript\Example\temp\game999_src1.cs ln:27 method:Boolean Boing(Int32)
                //fn:C:\Dev\Libs\NScript\Example\temp\game999_src0.cs ln:33 method:Int32 Setup(System.String)
                //fn: ln:0 method:System.Object InvokeMethod(System.Object, Void**, System.Signature, Boolean)
                //fn: ln:0 method:System.Object InvokeDirectByRefWithFewArgs(System.Object, System.Span`1[System.Object], System.Reflection.BindingFlags)
            }

            Environment.Exit(0);
        }
    }
}
