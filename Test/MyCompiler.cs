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
    /// <summary>xxx compiler.</summary>
    public class MyCompiler : CompilerCore
    {
        /// <summary>Channel info collected from the script.</summary>


        Color? _skyColor;
        DateTime? _start;

      //  public List<ChannelSpec> ChannelSpecs { get; init; } = [];

        /// <summary>Normal constructor.</summary>
        //public MyCompiler(string scriptPath)
        //{
        //    ScriptPath = scriptPath;
        //}



        //////////////////// CompilerCore.cs operation hooks /////////////////////



        /// <summary>Called before compiler starts.</summary>
        public override void PreCompile()
        {
        //    ChannelSpecs.Clear();

            LocalDlls = ["NAudio", "Ephemera.NBagOfTricks", "Ephemera.NebOsc", "Ephemera.MidiLib", "Nebulator.Script"];

            Usings.Add("System.Drawing");
            //Usings.Add("static Ephemera.NBagOfTricks.MusicDefinitions");

            //// Save hash of current channel descriptors to detect change in source code.
            //_chHash = string.Join("", _channelDescriptors).GetHashCode();
            //_channelDescriptors.Clear();
        }

        /// <summary>Called after compiler finished.</summary>
        public override void PostCompile()
        {
        }


        bool _doingGlobals = true;


        /// <summary>Called for each line in the source file before compiling.</summary>
        /// <param name="sline"></param>
        /// <param name="pcont"></param>
        /// <returns>True - exclude from output file</returns>
        public override bool PreprocessLine(string sline, FileContext pcont)
        {
            bool handled = false;

            if (!_doingGlobals) return handled;


            //        Color _skyColor = ???;
            //        DateTime _start = ???;
            //SkyColor = "green";
            //Start = DateTime.Now();


            try
            {
                var parts = sline.Replace("\"", "").SplitByToken("=");

                if (parts.Count == 2)
                {
                    switch (parts[0])
                    {
                        case "SkyColor":
                            _skyColor = Color.FromName(parts[1]);
                            handled = true;
                            break;

                        default:
                            // nothing of interest
                            break;
                    }
                }
            }
            catch (Exception)
            {
                Results.Add(new()
                {
                    ResultType = CompileResultType.Error,
                    Message = $"Bad statement:{sline}",
                    SourceFile = pcont.SourceFile,
                    LineNumber = pcont.LineNumber
                });
            }

            return handled;
        }
    }
}
