using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;

#pragma warning disable IDE0059 // Unnecessary assignment of a value


namespace Example
{
    class DevCompiler : CompilerCore { }


    // Loading collectible.
    // The following code is an example of the simplest custom AssemblyLoadContext:
    class TestAssemblyLoadContext : AssemblyLoadContext
    {
        public TestAssemblyLoadContext() : base(isCollectible: true)
        {
        }

        // As you can see, the Load method returns null. That means that all the dependency assemblies are
        // loaded into the default context, and the new context contains only the assemblies explicitly loaded into it.
        protected override Assembly? Load(AssemblyName name)
        {
            return null;
        }
    }


    public class Dev
    {
        string code = @"
            using System;
            namespace DontCare
            {
                public class Klass
                {
                    public int iii = 10;
                    public double RealTime { get; set; } = 100.0;
                    public int Dev(string s) { iii = s.Length; RealTime++; return iii; }
                }
            }";

        public void AssemblyPlay()
        {
            DevCompiler compiler = new()
            {
                IgnoreWarnings = true,
                Namespace = "DontCare",
                SystemDlls = ["System", "System.Private.CoreLib", "System.Runtime", "System.Collections"],
                //LocalDlls = ["Ephemera.NBagOfTricks", "Ephemera.NScript"],
                Usings = ["System.Collections.Generic", "System.Text"]
            };

            // What's loaded?
            HowMany("none");

            // Loads memory.
            var assy = compiler.CompileText(code);
            HowMany("first");

            // Again.
            assy = compiler.CompileText(code);
            HowMany("second");

            // Host applications could do something like this:
            void HowMany(string info)
            {
                var assys = AppDomain.CurrentDomain.GetAssemblies();
                var num = assys.Where(a => a.FullName.Contains(compiler.Namespace)).Count();
                Program.Print($"{info}:{num}");
            }
        }

        public void UnloadableAssemblyPlay()
        {
            // https://carljohansen.wordpress.com/2020/05/09/compiling-expression-trees-with-roslyn-without-memory-leaks-2/
            // Not for the Example (run to completion then exit) but yes for long-running apps that reload externally modified scripts.
            // Assemblies, once loaded, cannot be unloaded until the process shuts down or the AssemblyLoadContext is unloaded.
            // In  .NET Framework there's no way to unload, but in .NET Core you can use an alternate AssemblyLoadContext which if provided
            // can be used to unload assemblies loaded in the context conditionally.

            // https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability


            DevCompiler compiler = new()
            {
                IgnoreWarnings = true,
                Namespace = "DontCare",
                SystemDlls = ["System", "System.Private.CoreLib", "System.Runtime", "System.Collections"],
                //LocalDlls = ["Ephemera.NBagOfTricks", "Ephemera.NScript"],
                Usings = ["System.Collections.Generic", "System.Text"]
            };

            // https://github.com/dotnet/samples/tree/main/core/tutorials/Unloading

            // You can create an instance of the custom AssemblyLoadContext and load an assembly into it as follows:
            var alc = new TestAssemblyLoadContext();
            //Assembly a = alc.LoadFromAssemblyPath(assemblyPath);
            Assembly a = alc.LoadFromAssemblyPath("assemblyPath");

            // For each of the assemblies referenced by the loaded assembly, the TestAssemblyLoadContext.Load method
            // is called so that the TestAssemblyLoadContext can decide where to get the assembly from. In this case,
            // it returns null to indicate that it should be loaded into the default context from locations that the
            // runtime uses to load assemblies by default.

            // Now that an assembly was loaded, you can execute a method from it. Run the Main method:
            var args = new object[1] { new string[] { "Hello" } };
            _ = a.EntryPoint?.Invoke(null, args);

            // After the Main method returns, you can initiate unloading by either calling the Unload method on the
            // custom AssemblyLoadContext or removing the reference you have to the AssemblyLoadContext:
            alc.Unload();
            // This is sufficient to unload the test assembly. Next, you'll put all of this into a separate noninlineable
            // method to ensure that the TestAssemblyLoadContext, Assembly, and MethodInfo (the Assembly.EntryPoint) can't
            // be kept alive by stack slot references (real- or JIT-introduced locals). That could keep the TestAssemblyLoadContext
            // alive and prevent the unload.

            // Also, return a weak reference to the AssemblyLoadContext so that you can use it later to detect unload completion.
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ExecuteAndUnload(string assemblyPath, out WeakReference alcWeakRef)
            {
                // load
                var alc = new TestAssemblyLoadContext();
                Assembly a = alc.LoadFromAssemblyPath(assemblyPath);
                alcWeakRef = new WeakReference(alc, trackResurrection: true);
                // execute
                var args = new object[1] { new string[] { "Hello" } };
                _ = a.EntryPoint?.Invoke(null, args);
                // unload
                alc.Unload();
            }

            // Now you can run this function to load, execute, and unload the assembly.
            WeakReference testAlcWeakRef;
            ExecuteAndUnload("absolute/path/to/your/assembly", out testAlcWeakRef);

            // However, the unload doesn't complete immediately. As previously mentioned, it relies on the garbage collector to collect all the objects from the test assembly. In many cases, it isn't necessary to wait for the unload completion. However, there are cases where it's useful to know that the unload has finished. For example, you might want to delete the assembly file that was loaded into the custom AssemblyLoadContext from disk. In such a case, the following code snippet can be used. It triggers garbage collection and waits for pending finalizers in a loop until the weak reference to the custom AssemblyLoadContext is set to null, indicating the target object was collected. In most cases, just one pass through the loop is required. However, for more complex cases where objects created by the code running in the AssemblyLoadContext have finalizers, more passes might be needed.
            for (int i = 0; testAlcWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            // Can't unload issue with resolution: https://github.com/dotnet/roslyn/issues/49282
            // Create a CSharpCompilation CSharpCompilation.Create
            // Create a collectable AssemblyLoadContext (https://source.dot.net/#System.Private.CoreLib/AssemblyLoadContext.cs,418)
            // Emit the compilation to a MemoryStream, use AssemblyLoadContext.LoadFromStream to load it - getting an Assembly
            // Use Assembly.GetType, Activator.CreateInstance, compilation.GetEntryPoint...GetMethod etc to call
            //   the compiled stuff in the new assembly
            // Call AssemblyLoadContext.Unload in a finally block or employ a using statement to do so.
            // Assemblies remain loaded, repeat until all file handles are leaked and the process hangs/dies.
        }

        public void ReflectionPlay()
        {
            ///// Compile script with application options.
            GameCompiler compiler = new()
            {
                IgnoreWarnings = true,
                Namespace = "DontCare"
            };

            var assy = compiler.CompileText(code);
            Type? type = assy!.GetType("DontCare.Klass");
            object? inst = Activator.CreateInstance(type!);

            //https://learn.microsoft.com/en-us/dotnet/api/system.type.getmembers?view=net-9.0
            //https://learn.microsoft.com/en-us/dotnet/api/system.reflection.bindingflags?view=net-9.0

            foreach (MemberInfo m in type!.GetMembers())
            {
                var s = m.ToString();
                Console.WriteLine($"member: {m.MemberType} {m}"); // s = "Double get_RealTime()"
            }
            //member: Method Double get_RealTime()
            //member: Method Void set_RealTime(Double)
            //member: Method Int32 Dev(System.String)
            //member: Method System.Type GetType()
            //member: Method System.String ToString()
            //member: Method Boolean Equals(System.Object)
            //member: Method Int32 GetHashCode()
            //member: Constructor Void .ctor()
            //member: Property Double RealTime
            //member: Field Int32 iii

            // Methods
            var miDev = type!.GetMethod("Dev");
            var piTime = type.GetProperty("RealTime");
            var miTimeGet = piTime!.GetGetMethod();
            var miTimeSet = piTime.GetSetMethod();

            // Delegates
            var delDev = (Func<string, int>)Delegate.CreateDelegate(typeof(Func<string, int>), inst, miDev!);
            var delTimeGet = (Func<double>)Delegate.CreateDelegate(typeof(Func<double>), inst, miTimeGet!);
            var delTimeSet = (Action<double>)Delegate.CreateDelegate(typeof(Action<double>), inst, miTimeSet!);

            List<long> invoked = [];
            List<long> delegated = [];
            List<long> prop = [];

            for (int i = 0; i < 10; i++)
            {
                var start1 = Stopwatch.GetTimestamp();
                miDev!.Invoke(inst, ["xxx"]);
                invoked.Add(Stopwatch.GetTimestamp() - start1);

                var start2 = Stopwatch.GetTimestamp();
                delDev("xxx");
                delegated.Add(Stopwatch.GetTimestamp() - start2);

                var start3 = Stopwatch.GetTimestamp();
                //piTime.GetValue(inst);
                delTimeGet();
                prop.Add(Stopwatch.GetTimestamp() - start3);

                //System.Threading.Thread.Sleep(50);
            }

            // Examine effects.
            // property delegates: using the results of the GetGetMethod and GetSetMethod methods of PropertyInfo.
            var ntime = piTime.GetValue(inst);

            for (int i = 0; i < invoked.Count; i++)
            {
                Console.WriteLine($"iter{i} invoked:{invoked[i]} delegated:{delegated[i]} prop:{prop[i]}");
            }
        }
    }
}
