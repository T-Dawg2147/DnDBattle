using DnDBattle.Models;
using DnDBattle.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using DnDBattle.Services;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Services.Combat
{
    public class InitiativeResult
    {
        public Token Token { get; set; }
        public int Roll { get; set; }
        public int Modifier { get; set; }
        public int Total => Roll + Modifier;
    }

    public class InitiativeManager
    {
        private readonly IList<Token> _participants;
        private readonly Random _rng = new Random();

        public List<InitiativeResult> LastRolls { get; } = new List<InitiativeResult>();

        public InitiativeManager(IList<Token> participants)
        {
            _participants = participants;
            CurrentIndex = -1;
        }

        public IList<Token> Order
        {
            get
            {
                return _participants
                    .OrderByDescending(t => t.Initiative)
                    .ThenBy(t => _rng.Next())
                    .ToList();
            }
        }
        

        public int CurrentIndex { get; private set; } = -1;
        public Token CurrentToken
        {
            get
            {
                var ord = Order;
                if (ord.Count == 0 || CurrentIndex < 0 || CurrentIndex >= ord.Count) return null;
                return ord[CurrentIndex];
            }
        }

        public InitiativeResult RollInitiativeSingle(Token t)
        {
            var dice = DiceRoller.RollExpression("1d20");
            int roll = dice.Total;
            int mod = t.InitiativeModifier;
            var res = new InitiativeResult { Token = t, Roll = roll, Modifier = mod };
            t.Initiative = res.Total;
            LastRolls.Add(res);
            return res;
        }

        public List<InitiativeResult> RollAll(Func<Token, (int roll, int modifier)> roller = null)
        {
            LastRolls.Clear();
            foreach (var t in _participants)
            {
                int roll = DiceRoller.RollExpression("1d20").Total;
                int mod = t.InitiativeModifier;
                if (roller != null)
                {
                    var custom = roller(t);
                    roll = custom.roll;
                    mod = custom.modifier;
                }
                t.Initiative = roll + mod;
                LastRolls.Add(new InitiativeResult { Token = t, Roll = roll, Modifier = mod });
            }
            CurrentIndex = (_participants.Count > 0) ? 0 : -1;
            return LastRolls.ToList();
        }

        public void NextTurn()
        {
            var ord = Order;
            if (ord.Count == 0) { CurrentIndex = -1; return; }
            CurrentIndex = (CurrentIndex + 1) % ord.Count;
        }

        public void Reset()
        {
            CurrentIndex = -1;
            foreach (var t in _participants) t.Initiative = 0;
            LastRolls.Clear();
        }
    }
}
