using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.IO;

#nullable enable

namespace NScript.Example
{
    /// <summary>Player roles.</summary>
    public enum Role { Oligarch, Sycophant, Hero, Peon }

    public class GameScriptBase
    {
        #region Fields
        /// <summary>Roll the dice.</summary>
        readonly Random rand = new();

        TextWriter? writeStream;
        #endregion
 
        #region Properties - dynamic things shared between host and script at runtime TODO1? should be Globals not static
        /// <summary>All the players. Key is name.</summary>
        public static Dictionary<string, Role> Players { get; } = [];

        /// <summary>Board size.</summary>
        public static int WorldX { get; set; } = 50;

        /// <summary>Board size.</summary>
        public static int WorldY { get; set; } = 50;

        /// <summary>Main -> Script</summary>
        public static double RealTime { get; set; } = 0.0;
        #endregion

        #region Internal functions
        public void Init(TextWriter stream)
        {
            writeStream = stream;
        }
        #endregion

        #region Host application calls game functions
        /// <summary>Initialization.</summary>
        public virtual int Setup(string info)
        {
            throw new NotImplementedException();
        }

        /// <summary>Do something.</summary>
        public virtual int Move()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Common game functions
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
