//using Ephemera.NBagOfTricks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Ephemera.NScript.Example
{
    public enum Role { Oligarch, Sycophant, Rebel, Peon }

    public class MyScriptApi
    {
        #region Fields accessible by game implementations
        protected int worldX;

        protected int worldY;

        protected Dictionary<string, Role> players = [];

        readonly Random rand = new();
        #endregion

        #region Application => Game

        public virtual int Setup()
        {
            throw new NotImplementedException();
        }

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

        protected void Print(string msg)
        {
            Console.WriteLine($"Script Print(): {msg}");
        }

        protected string RandomPlayer()
        {
            Print($"RandomPlayer: {players.Count}");
            int i = rand.Next(0, players.Count);
            var player = players.ToList()[i];
            return player.Key;
        }
        #endregion
    }
}
