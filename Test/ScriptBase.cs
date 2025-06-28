using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NScript;


namespace Test
{
    /////////////////// All the stuff to do app specific work ///////////////////
    // Define script type api - as virtual members
    // Store script data
    // Do runtime work/processing.

    public enum Role { Oligarch, Sycophant, Rebel, Peon }


    //protected partial class ScriptBase
    public class ScriptBase
    {
        #region Fields accessible by implementations
        protected int worldX;

        protected int worldY;

        protected Dictionary<string, Role> players = [];
        #endregion

        #region Application => Game

        public virtual int Setup()
        {
            throw new NotImplementedException();
        }

        // protected int MovePlayer(Role which, string location, int strength)
        public virtual int Move()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Game => Application
        protected void CreatePlayer(Role role, string name)
        {
            players.Add(name, role);
        }


        // protected virtual int Negotiate(Role with)
        // {
        //     //throw new NotImplementedException();
        // }

        // protected virtual int Invade(string location)
        // {
        //     //throw new NotImplementedException();
        // }
        #endregion
    }
}
