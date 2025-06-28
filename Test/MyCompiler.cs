using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;


namespace Test
{
    /// <summary>Test compiler.</summary>
    public class MyCompiler : CompilerCore
    {
        /// <summary>Info collected from the script.</summary>
        bool _gotBurger = false;

        #region CompilerCore operation hooks
        /// <summary>Called before compiler starts.</summary>
        public override void PreCompile()
        {
            LocalDlls = ["Ephemera.NBagOfTricks", "Ephemera.NScript"];
            Usings.Add("System.Drawing");
        }

        /// <summary>Called after compiler finished.</summary>
        public override void PostCompile()
        {
            if (_gotBurger)
            {
                Console.WriteLine("Got a burger!!!");
            }
        }

        /// <summary>Called for each line in the source file before compiling.</summary>
        /// <param name="sline"></param>
        /// <param name="pcont"></param>
        /// <returns>True - exclude from output file</returns>
        public override bool PreprocessLine(string sline, FileContext pcont)
        {
            bool handled = false;

            var parts = sline.SplitByToken("=>");
            if (parts.Count == 2 && parts[0] == "kustom")
            {
                _gotBurger = true;
                handled = true;
            }

            return handled;
        }
        #endregion
    }
}
