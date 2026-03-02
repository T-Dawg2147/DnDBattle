using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using Action = DnDBattle.Models.Combat.Action;

namespace DnDBattle.Models.Encounters
{
    public class InitiativeEntry : ObservableObject
    {
        private Token _token;
        public Token Token
        {
            get => _token;
            set => SetProperty(ref _token, value);
        }

        private int _initiativeRoll;
        public int InitiativeRoll
        {
            get => _initiativeRoll;
            set => SetProperty(ref _initiativeRoll, value);
        }

        private int _initiativeTotal;
        public int InitiativeTotal
        {
            get => _initiativeTotal;
            set => SetProperty(ref _initiativeTotal, value);
        }

        private bool _isCurrentTurn;
        public bool IsCurrentTurn
        {
            get => _isCurrentTurn;
            set => SetProperty(ref _isCurrentTurn, value);
        }

        private bool _isDelaying;
        public bool IsDelaying
        {
            get => _isDelaying;
            set => SetProperty(ref _isDelaying, value);
        }

        private bool _isReadying;
        public bool IsReadying
        {
            get => _isReadying;
            set => SetProperty(ref _isReadying, value);
        }

        private bool _hasActed;
        public bool HasActed
        {
            get => _hasActed;
            set => SetProperty(ref _hasActed, value);
        }

        private string _readiedAction;
        public string ReadiedAction
        {
            get => _readiedAction;
            set => SetProperty(ref _readiedAction, value);
        }

        public bool IsPlayer => Token?.IsPlayer ?? false;

        public string DisplayName => Token?.Name ?? "Unknown";

        public string StatusText
        {
            get
            {
                if (IsDelaying) return "Delaying";
                if (IsReadying) return $"Readying: {ReadiedAction ?? "Action"}";
                if (HasActed) return "Dont";
                return "";
            }
        }

        public InitiativeEntry() { }

        public InitiativeEntry(Token token) 
        {
            Token = token;
            InitiativeRoll = 0;
            InitiativeTotal = token?.Initiative ?? 0;
        }

        public InitiativeEntry(Token token, int roll)
        {
            Token = token;
            InitiativeRoll = roll;
            InitiativeTotal = roll + (token?.InitiativeModifier ?? 0);
            if (token != null)
                token.Initiative = InitiativeTotal;
        }

        public void ResetForNewRound()
        {
            HasActed = false;
            IsDelaying = false;
        }
    }
}
