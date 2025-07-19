using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;
using Example.Script;


namespace Example
{
    /// <summary>Example starts here.</summary>
    internal class Program
    {
        static void Main()
        {
            //_ = Utils.WarmupRoslyn();

            /////////////////////////////////////////////////////
            //var dev = new Dev();
            ////dev.ReflectionPlay();
            //dev.AssemblyPlay();
            //Environment.Exit(0);

            /////////////////////////////////////////////////////
            var app = new Example();
            var ret = -1;

            /////////////////////////////////////////////////////
            ret = app.RunByReflection();
            if (ret > 0)
            {
                Console.WriteLine($"!!! App failed with {ret}");
            }

            /////////////////////////////////////////////////////
            ret = app.RunByBinding();
            if (ret > 0)
            {
                Console.WriteLine($"!!! App failed with {ret}");
            }

            Environment.Exit(ret);
        }

        /// <summary>Tell the user something.</summary>
        public static void Print(string msg)
        {
            Console.WriteLine($">>> {msg}");
        }
    }
}
