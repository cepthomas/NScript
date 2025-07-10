using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.IO;

#nullable enable

namespace Example.Script
{
    public class ScriptBase
    {
        #region Types
        /// <summary>Player roles.</summary>
        public enum Role { Oligarch, Sycophant, Hero, Peon }
        #endregion

        #region Fields
        /// <summary>Roll the dice.</summary>
        readonly Random rand = new();

        /// <summary>Host supplied output.</summary>
        TextWriter? writeStream;
        #endregion
 
        #region Properties - accessible by host and script
        // TODOX Statics probably should be proper globals.
        
        /// <summary>All the players. Key is name.</summary>
        public static Dictionary<string, Role> Players { get; } = [];

        /// <summary>Board size.</summary>
        public static int WorldX { get; set; } = 50;

        /// <summary>Board size.</summary>
        public static int WorldY { get; set; } = 50;

        /// <summary>Main -> Script</summary>
        public static double RealTime { get; set; } = 0.0;
        #endregion

        #region Public functions - called by host
        /// <summary>Internal script initialization.</summary>
        public void Init(TextWriter stream)
        {
            writeStream = stream;
        }

        /// <summary>Required script function.</summary>
        public virtual int Setup(string info)
        {
            throw new NotImplementedException();
        }

        /// <summary>Required script function.</summary>
        public virtual int Move()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Game functions - called by script
        /// <summary>Make me a player.</summary>
        protected void CreatePlayer(Role role, string name)
        {
            Print($"CreatePlayer({role}, {name}) {Players.Count}");
            Players.Add(name, role);
        }

        /// <summary>Roll the dice.</summary>
        protected string RandomPlayer()
        {
            if (Players.Count > 0)
            {
                int i = rand.Next(0, Players.Count);
                //i = 99;
                var player = Players.ToList()[i];
                Print($"RandomPlayer: {player.Key}");
                return player.Key;
            }
            else
            {
                return "All players dead!";
            }
        }

        /// <summary>Tell the user something.</summary>
        protected void Print(string msg)
        {
            writeStream?.WriteLine(msg);
        }
        #endregion
    }
}
