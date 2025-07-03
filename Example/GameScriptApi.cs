using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;


namespace Ephemera.NScript.Example
{
    /// <summary>Player roles.</summary>
    public enum Role { Oligarch, Sycophant, Hero, Peon }

    public class GameScriptApi
    {
        #region Fields accessible by game implementations
        /// <summary>All the players. Key is name.</summary>
        protected Dictionary<string, Role> players = [];

        /// <summary>Board size.</summary>
        protected int worldX;

        /// <summary>Board size.</summary>
        protected int worldY;

        /// <summary>The dice.</summary>
        readonly Random rand = new();
        #endregion

        public event EventHandler<string>? PrintMessage;


        #region Host application calls game functions
        /// <summary>Initialization.</summary>
        public virtual int Setup()
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
            Print($"CreatePlayer({role}, {name}) {players.Count}");
            players.Add(name, role);
        }

        /// <summary>Roll the dice.</summary>
        protected string RandomPlayer()
        {
            Print($"RandomPlayer: {players.Count}");
            int i = rand.Next(0, players.Count);
            var player = players.ToList()[i];
            Print($"RandomPlayer: {player.Key}");
            return player.Key;
        }

        /// <summary>Tell the user something.</summary>
        protected void Print(string msg)
        {
            PrintMessage?.Invoke(this, msg);
            //Console.WriteLine($">>>>>> Console >>>>>>>>> Script: {msg} {GetHashCode()}"); //TODO1 can't find Console??
        }
        #endregion
    }
}
