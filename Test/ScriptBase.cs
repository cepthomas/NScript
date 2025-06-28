using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.ScriptCompiler;


namespace Test
{
    /////////////////// All the stuff to do app specific work ///////////////////
    // Define my script type api - as virtual members
    // Store script data
    // Do runtime work/processing.

    public enum Player { Oligarch, Sycophant, Rebel, Peon }

    //public partial class ScriptBase
    public class ScriptBase
    {
        #region Properties - dynamic things shared between host and script at runtime
        /// <summary>Main -> Script</summary>
        public double RealTime { get; set; } = 0.0;

        #endregion


        public virtual int Setup(int worldX, int worldY)
        {
            throw new NotImplementedException();
        }

        public virtual int CreatePlayer(Player which)
        {
            throw new NotImplementedException();
        }

        public virtual int MovePlayer(Player which, string location, int strength)
        {
            throw new NotImplementedException();
        }

        public virtual int Negotiate(Player with)
        {
            throw new NotImplementedException();
        }

        public virtual int Invade(string location)
        {
            throw new NotImplementedException();
        }


        // /// <summary>
        // /// Set up runtime stuff.
        // /// </summary>
        // /// <param name="channels">All output channels.</param>
        // public void Init(Dictionary<string, Channel> channels)
        // {
        //     _channels = channels;
        // }

    }
}
