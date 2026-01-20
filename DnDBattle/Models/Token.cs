using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Windows.Media;

namespace DnDBattle.Models
{
    public class Token : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Desriptive fields
        private string _name = "Token";
        public string Name{ get => _name; set => SetProperty(ref _name, value); }

        private string _size;
        public string Size { get => _size; set => SetProperty(ref _size, value); }

        private string _type;
        public string Type { get => _type; set => SetProperty(ref _type, value); }

        private string _alignment;
        public string Alignment { get => _alignment; set => SetProperty(ref _alignment, value); }

        private string _challengeRating;
        public string ChallengeRating { get => _challengeRating; set => SetProperty(ref _challengeRating, value); }

        private bool _isPlayer = false;
        public bool IsPlayer { get => _isPlayer; set => SetProperty(ref _isPlayer, value); }

        // Defense / HP
        private int _armorClass;
        public int ArmorClass { get => _armorClass; set => SetProperty(ref _armorClass, value); }

        private int _maxHP;
        public int MaxHP { get => _maxHP; set => SetProperty(ref _maxHP, value); }

        private string _hitDice;
        public string HitDice { get => _hitDice; set => SetProperty(ref _hitDice, value); }

        private int _initiativeModifier;
        public int InitiativeModifier { get => _initiativeModifier; set => SetProperty(ref _initiativeModifier, value); }

        private int _initiative;
        public int Initiative { get => _initiative; set => SetProperty(ref _initiative, value); }

        private string _speed;
        public string Speed { get => _speed; set => SetProperty(ref _speed, value); }

        // Current HP
        private int _hp;
        public int HP { get => _hp; set => SetProperty(ref _hp, value); }

        // Ability stats
        private int _str = 10;
        public int Str { get => _str; set => SetProperty(ref _str, value); }

        private int _dex;
        public int Dex { get => _dex; set => SetProperty(ref _dex, value); }

        private int _con;
        public int Con { get => _con; set => SetProperty(ref _con, value); }

        private int _int;
        public int Int { get => _int; set => SetProperty(ref _int, value); }

        private int _wis;
        public int Wis { get => _wis; set => SetProperty(ref _wis, value); }

        private int _cha;
        public int Cha { get => _cha; set => SetProperty(ref _cha, value); }

        #region Monement Tracking

        private int _gridX;
        public int GridX { get => _gridX; set => SetProperty(ref _gridX, value); }

        private int _gridY;
        public int GridY { get => _gridY; set => SetProperty(ref _gridY, value); }

        public int SizeInSquares { get; set; } = 1;

        private int _movementUsedThisTurn;
        public int MovementUsedThisTurn
        {
            get => _movementUsedThisTurn;
            set
            {
                _movementUsedThisTurn = value;
                OnPropertyChanged(nameof(MovementUsedThisTurn));
                OnPropertyChanged(nameof(MovementRemainingThisTurn));
                OnPropertyChanged(nameof(CanMoveThisTurn));
                OnPropertyChanged(nameof(MovementStatusText));
            }
        }

        public int MovementRemainingThisTurn => Math.Max(0, SpeedSquares - MovementUsedThisTurn);

        public bool CanMoveThisTurn => MovementRemainingThisTurn > 0;

        public string MovementStatusText => $"{MovementRemainingThisTurn} / {SpeedSquares} squares";

        public void ResetMovementForNewTurn() =>
            MovementUsedThisTurn = 0;

        public bool TryUseMovement(int squares)
        {
            if (squares <= MovementRemainingThisTurn)
            {
                MovementUsedThisTurn += squares;
                return true;
            }
            return false;
        }

        #endregion

        // Traits/features (will remain text to allow anything as input)
        private string _traits;
        public string Traits { get => _traits; set => SetProperty(ref _traits, value); }

        // Action lists
        public List<Action> Actions { get; set; } = new List<Action>();
        public List<Action> BonusActions { get; set; } = new List<Action>();
        public List<Action> Reactions { get; set; } = new List<Action>();
        public List<Action> LegendaryActions { get; set; } = new List<Action>();

        // Notes
        private string _notes;
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }        

        // Extra fields (adds context)
        public List<string> Skills { get; set; } = new List<string>();

        private string _senses;
        public string Senses { get => _senses; set => SetProperty(ref _senses, value); }

        private string _languages;
        public string Languages { get => _languages; set => SetProperty(ref _languages, value); }

        private string _immunities;
        public string Immunities { get => _immunities; set => SetProperty(ref _immunities, value); }

        private string _resistances;
        public string Resistances { get => _resistances; set => SetProperty(ref _resistances, value); }

        private string _vulnerabilities;
        public string Vulnerabilities { get => _vulnerabilities; set => SetProperty(ref _vulnerabilities, value); }

        private bool _isFavorite;
        public bool IsFavorite { get => _isFavorite; set { _isFavorite = value; OnPropertyChanged(nameof(IsFavorite)); } }

        // === Combat Tracking Properties ===

        // Current Turn
        private bool _isCurrentTurn;
        public bool IsCurrentTurn
        {
            get => _isCurrentTurn;
            set
            {
                _isCurrentTurn = value;
                OnPropertyChanged(nameof(IsCurrentTurn));
            }
        }

        // Conditions
        private Condition _conditions = Condition.None;
        public Condition Conditions
        {
            get => _conditions;
            set => SetProperty(ref _conditions, value);
        }

        // Death Saving Throws
        private int _deathSaveSuccesses = 0;
        public int DeathSaveSuccesses
        {
            get => _deathSaveSuccesses;
            set => SetProperty(ref _deathSaveSuccesses, value);
        }

        private int _deathSaveFailures = 0;
        public int DeathSaveFailures
        {
            get => _deathSaveFailures;
            set => SetProperty(ref _deathSaveFailures, value);
        }

        // Temporary HP
        private int _tempHP = 0;
        public int TempHP
        {
            get => _tempHP;
            set => SetProperty(ref _tempHP, value);
        }

        // Concentration spell name
        private string _concentrationSpell;
        public string ConcentrationSpell
        {
            get => _concentrationSpell;
            set => SetProperty(ref _concentrationSpell, value);
        }        

        // Icon path
        private ImageSource _image;
        public ImageSource Image { get => _image; set => SetProperty(ref _image, value); }

        private string _iconPath;
        public string IconPath { get => _iconPath; set => SetProperty(ref _iconPath, value); }

        public int SpeedSquares
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Speed)) return 6;
                var parts = Speed.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[0], out int ft)) return Math.Max(1, ft / 5);
                return 6;
            }
        }

        // Extras
        public Dictionary<string, object> Extras { get; set; } = new Dictionary<string, object>();

        public List<string> Tags { get; set; } = new List<string>();

        public List<string> EncounterTags { get; set; } = new List<string>();

        // Helper Properties
        public bool IsUnconscious => HP <= 0 && !IsPlayer;
        public bool IsDying => HP <= 0 && IsPlayer && DeathSaveSuccesses < 3 && DeathSaveFailures < 3;
        public bool IsStable => HP <= 0 && IsPlayer && DeathSaveSuccesses >= 3;
        public bool IsDead => (HP <= 0 && IsPlayer && DeathSaveFailures >= 3) || HP <= -MaxHP;

        public string ConditionsDisplay => Conditions.ToDisplayString();

        // Helper Methods
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        public bool HasEncounterTag(string tag) => EncounterTags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public bool HasCondition(Condition condition) => Conditions.HasFlag(condition);

        public void AddCondition(Condition condition)
        {
            Conditions |= condition;
            OnPropertyChanged(nameof(ConditionsDisplay));
        }

        public void RemoveCondition(Condition condition)
        {
            Conditions &= ~condition;
            OnPropertyChanged(nameof(ConditionsDisplay));
        }

        public void ToggleCondition(Condition condition)
        {
            if (HasCondition(condition))
                RemoveCondition(condition);
            else
            {
                AddCondition(condition);
            }
        }

        public void ResetDeathSaves()
        {
            DeathSaveSuccesses = 0;
            DeathSaveFailures = 0;
        }
    }
}
