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


namespace NScript.Example
{
    /// <summary>Test compiler.</summary>
    public class GameCompiler : CompilerCore
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
}
