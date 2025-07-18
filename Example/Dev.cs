using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;

#pragma warning disable IDE0059 // Unnecessary assignment of a value


TODOX does this apply? https://carljohansen.wordpress.com/2020/05/09/compiling-expression-trees-with-roslyn-without-memory-leaks-2/
https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability

// Not for the Example (run to completion then exit) but yes for long-running apps that reload externally modified scripts.
// Assemblies, once loaded, cannot be unloaded until the process shuts down or the AssemblyLoadContext is unloaded.
// In  .NET Framework there's no way to unload, but in .NET Core you can use an alternate AssemblyLoadContext which if provided
// can be used to unload assemblies loaded in the context conditionally.


namespace Example
{
    public class Dev //TODOX do something??
    {
        /// <summary>Used to investigate reflection options.</summary>
        /// <returns></returns>
        public void Explore()
        {
            ///// Compile script with application options.
            GameCompiler compiler = new()
            {
                ScriptPath = ".",
                IgnoreWarnings = true,
                Namespace = "DontCare"
            };

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

            var assy = compiler.CompileText(code);
            object? inst = null;
            Type? type = assy!.GetType("DontCare.Klass");
            inst = Activator.CreateInstance(type!);

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
