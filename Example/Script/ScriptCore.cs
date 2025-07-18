using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.IO;

#nullable enable

namespace Example.Script
{
    public class ScriptCore
    {
        #region Types
        /// <summary>Player roles.</summary>
        public enum Role { Oligarch, Sycophant, Hero, Peon }
        #endregion

        #region Fields
        /// <summary>Roll the dice.</summary>
        readonly Random _rand = new();

        /// <summary>Host supplied output.</summary>
        TextWriter? _writeStream;

        /// <summary>All the players. Key is name.</summary>
        Dictionary<string, Role> _players = [];

        /// <summary>Board size.</summary>
        protected int _worldX = 50;

        /// <summary>Board size.</summary>
        protected int _worldY = 50;
        #endregion

        #region Properties - accessible by host and script
        /// <summary>When are we.</summary>
        public double RealTime { get; set; } = 0.0;
        #endregion

        #region Public functions - called by host
        /// <summary>Internal script initialization.</summary>
        public int Init(TextWriter stream)
        {
            _writeStream = stream;
            return 0;
        }

        /// <summary>Required script function.</summary>
        public virtual int Setup(string info, int worldX, int worldY)
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
            //Print($"CreatePlayer({role}, {name}) -> _players[{_players.Count}]");
            _players.Add(name, role);
        }

        /// <summary>Roll the dice. Really crude but we aren't using linq.</summary>
        protected string? RandomPlayer()
        {
            int r = _rand.Next(0, _players.Count);
            int i = 0;
            foreach (var p in _players) { if (i++ == r) return p.Key; }
            return null; // failed
        }

        /// <summary>What do they do?</summary>
        protected Role? GetRole(string player)
        {
            if ( _players.ContainsKey(player)) { return _players[player]; }
            return null;
        }

        /// <summary>Take them away.</summary>
        protected void Remove(string player)
        {
            _players.Remove(player);
        }

        /// <summary>Tell the user something.</summary>
        protected void Print(string msg)
        {
            _writeStream?.WriteLine($"SCR: {msg}");
        }
        #endregion
    }
}
