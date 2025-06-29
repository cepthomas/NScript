using System;
using System.Collections.Generic;
using System.Text;


namespace Ephemera.NScript.Test
{
    public enum Role { Oligarch, Sycophant, Rebel, Peon }

    public class ScriptBase
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

        protected int RandomInt(int min, int max)
        {
            return rand.Next(min, max);
        }

        protected Role RandomRole()
        {
            int r = rand.Next((int)Role.Oligarch, (int)Role.Peon);
            return (Role)r;
        }
        #endregion
    }
}
