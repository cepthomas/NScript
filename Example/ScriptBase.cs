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
        readonly Random _rand = new();

        /// <summary>Host supplied output.</summary>
        TextWriter? _writeStream;

        /// <summary>All the players. Key is name.</summary>
        protected Dictionary<string, Role> _players { get; } = [];

        /// <summary>Board size.</summary>
        protected int _worldX = 50;

        /// <summary>Board size.</summary>
        protected int _worldY = 50;
        #endregion

        #region Properties - accessible by host and script
        /// <summary>A property.</summary>
        public double RealTime { get; set; } = 0.0;
        #endregion

        #region Public functions - called by host
        /// <summary>Internal script initialization.</summary>
        public void Init(TextWriter stream)
        {
            _writeStream = stream;
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
            Print($"CreatePlayer({role}, {name}) {_players.Count}");
            _players.Add(name, role);
        }

        /// <summary>Roll the dice.</summary>
        protected string RandomPlayer()
        {
            if (_players.Count > 0)
            {
                int i = _rand.Next(0, _players.Count);
                //i = 99;
                var player = _players.ToList()[i];
                Print($"RandomPlayer: {player.Key}");
                return player.Key;
            }
            else
            {
                return "";
            }
        }

        /// <summary>Tell the user something.</summary>
        protected void Print(string msg)
        {
            _writeStream?.WriteLine($"SCR: {msg}");
        }
        #endregion
    }
}
