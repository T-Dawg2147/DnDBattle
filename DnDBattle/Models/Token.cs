using CommunityToolkit.Mvvm.ComponentModel;
using DnDBattle.Services;
using System;
using System.ComponentModel;
using System.Data.SQLite;
using System.Windows;
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

        private int _strMod = 0;
        public int StrMod { get => _strMod; set => SetProperty(ref _strMod, value); }

        private int _dex;
        public int Dex { get => _dex; set => SetProperty(ref _dex, value); }

        private int _dexMod = 0;
        public int DexMod { get => _dexMod; set => SetProperty(ref _dexMod, value); }

        private int _con;
        public int Con { get => _con; set => SetProperty(ref _con, value); }

        private int _conMod = 0;
        public int ConMod { get => _conMod; set => SetProperty(ref _conMod, value); }

        private int _int;
        public int Int { get => _int; set => SetProperty(ref _int, value); }

        private int _intMod = 0;
        public int IntMod { get => _intMod; set => SetProperty(ref _intMod, value); }

        private int _wis;
        public int Wis { get => _wis; set => SetProperty(ref _wis, value); }

        private int _wisMod = 0;
        public int WisMod { get => _wisMod; set => SetProperty(ref _wisMod, value); }

        private int _cha;
        public int Cha { get => _cha; set => SetProperty(ref _cha, value); }

        private int _chaMod = 0;
        public int ChaMod { get => _chaMod; set => SetProperty(ref _chaMod, value); }

        private int _passivePerception;
        public int PassivePerception { get => _passivePerception; set => SetProperty(ref _passivePerception, value); }

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
            set
            {
                if (_conditions != value)
                {
                    SetProperty(ref _conditions, value);
                    OnPropertyChanged(nameof(Conditions));
                }
            }
        }

        // Temporary HP
        private int _tempHP = 0;
        public int TempHP
        {
            get => _tempHP;
            set => SetProperty(ref _tempHP, value);
        }

        #region Image States

        private string _iconPath;
        public string IconPath { get => _iconPath; set => SetProperty(ref _iconPath, value); }

        // Old ImageSource
        private ImageSource _image;
        public ImageSource Image { get => _image; set => SetProperty(ref _image, value); }
        // New ImageSource  
        private ImageSource _displayImage;
        public ImageSource DisplayImage
        {
            get
            {
                if (_displayImage != null)
                    return _displayImage;

                if (Image != null)
                    return null;

                _displayImage = CreatureImageService.GetCreatureImageSync(
                    Name, Type, Size, ChallengeRating, IconPath);
                return _displayImage;
            }
            set => SetProperty(ref _displayImage, value);
        }
        
        public async Task RefreshImageAsync()
        {
            var newImage = await CreatureImageService.GetCreatureImageAsync(
                Name, Type, Size, ChallengeRating, IconPath);

            Application.Current.Dispatcher.Invoke(() =>
            {
                _displayImage = newImage;
                OnPropertyChanged(nameof(DisplayImage));
            });
        }

        public void ClearImageCache()
        {
            CreatureImageService.ClearCache(Name);
            _displayImage = null;
            OnPropertyChanged(nameof(DisplayImage));
        }

        #endregion

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

        #region Damage Type Properties

        public DamageType DamageImmunities => DamageTypeExtensions.ParseFromString(Immunities);

        public DamageType DamageResistances => DamageTypeExtensions.ParseFromString(Resistances);

        public DamageType DamageVulnerabilities => DamageTypeExtensions.ParseFromString(Vulnerabilities);

        public (int effectiveDamage, string description) CalculateEffectiveDamage(int baseDamage, DamageType damageType)
        {
            if ((DamageImmunities & damageType) != 0)
            {
                return (0, $"Immune to {damageType.GetDisplayName()}");
            }

            if ((DamageVulnerabilities & damageType) != 0)
            {
                int doubleDamage = baseDamage * 2;
                return (doubleDamage, $"Vulnerable to {damageType.GetDisplayName()} (×2)");
            }

            if ((DamageResistances & damageType) != 0)
            {
                int halfDamage = baseDamage / 2;
                return (halfDamage, $"Resistant to {damageType.GetDisplayName()} (½)");
            }

            return (baseDamage, null);
        }

        public (int damageTaken, string description) TakeDamage(int baseDamage, DamageType damageType)
        {
            var (effectiveDamage, description) = CalculateEffectiveDamage(baseDamage, damageType);

            if (TempHP > 0)
            {
                if (TempHP >= effectiveDamage)
                {
                    TempHP -= effectiveDamage;
                    string tempDesc = description != null ? $"{description}, absorbed by Temp HP" : "Absorbed by Temp HP";
                    return (0, tempDesc);
                }
                else
                {
                    int remaining = effectiveDamage - TempHP;
                    TempHP = 0;
                    HP = Math.Max(0, HP - remaining);
                    string tempDesc = description != null ? $"{description}, partially abosrbed by Temp HP" : "Partially absorbed by Temp HP";
                    return (remaining, tempDesc);
                }
            }

            HP = Math.Max(0, HP - effectiveDamage);
            return (effectiveDamage, description);
        }

        #endregion

        #region Legendary Actions

        private int _legendaryActionsMax = 0;
        public int LegendaryActionsMax
        {
            get => _legendaryActionsMax;
            set
            {
                if (SetProperty(ref _legendaryActionsMax, value))
                {
                    OnPropertyChanged(nameof(HasLegendaryActions));
                    OnPropertyChanged(nameof(LegendaryActionsDisplay));
                }
            }
        }

        private int _legendaryActionsRemaining = 0;
        public int LegendaryActionsRemaining
        {
            get => _legendaryActionsRemaining;
            set
            {
                SetProperty(ref _legendaryActionsRemaining, Math.Clamp(value, 0, LegendaryActionsMax));
                OnPropertyChanged(nameof(LegendaryActionsDisplay));
            }
        }

        public bool HasLegendaryActions => LegendaryActionsMax > 0;
        public string LegendaryActionsDisplay => $"{LegendaryActionsRemaining}/{LegendaryActionsMax}";

        public bool UseLegendaryAction(int cost = 1)
        {
            if (LegendaryActionsRemaining >= cost)
            {
                LegendaryActionsRemaining -= cost;
                return true;
            }
            return false;
        }

        public void ResetLegendaryActions()
        {
            LegendaryActionsRemaining = LegendaryActionsMax;
        }

        #endregion

        #region Lair Actions

        private bool _hasLairActions = false;
        public bool HasLairActions
        {
            get => _hasLairActions;
            set => SetProperty(ref _hasLairActions, value);
        }

        private string _lairActionDescription;
        public string LairActionDescription
        {
            get => _lairActionDescription;
            set => SetProperty(ref _lairActionDescription, value);
        }

        private bool _lairActionsUsedThisRound = false;
        public bool LairActionUserThisRound
        {
            get => _lairActionsUsedThisRound;
            set => SetProperty(ref _lairActionsUsedThisRound, value);
        }

        #endregion

        #region Concentration Tracking

        private bool _isConcentrating;
        public bool IsConcentrating
        {
            get => _isConcentrating;
            set
            {
                if (SetProperty(ref _isConcentrating, value))
                {
                    if (!value)
                    {
                        ConcentrationSpell = null;     
                    }
                    OnPropertyChanged(nameof(ConcentrationStatusText));
                }
            }
        }

        private string _concentrationSpell;
        public string ConcentrationSpell
        {
            get => _concentrationSpell;
            set
            {
                SetProperty(ref _concentrationSpell, value);
                OnPropertyChanged(nameof(ConcentrationStatusText));
            }
        }

        public string ConcentrationStatusText => IsConcentrating
            ? $"Concentrating: {ConcentrationSpell ?? "Unknown Spell"}"
            : null;

        public int ConcentrationSaveModifier => (Con - 10) / 2;

        public static int CalculateConcentrationDC(int damageTaken) =>
            Math.Max(10, damageTaken / 2);

        public void BreakConcentration()
        {
            string spell = ConcentrationSpell;
            IsConcentrating = false;
            ConcentrationSpell = null;
        }

        public void SetConcentration(string spellName)
        {
            IsConcentrating = true;
            ConcentrationSpell = spellName;
        }

        #endregion

        #region Death Saves

        private int _deathSaveSuccesses;
        public int DeathSaveSuccesses
        {
            get => _deathSaveSuccesses;
            set
            {
                SetProperty(ref _deathSaveSuccesses, Math.Clamp(value, 0, 3));
                OnPropertyChanged(nameof(DeathSaveStatusText));
                OnPropertyChanged(nameof(IsStabilized));
                OnPropertyChanged(nameof(IsDead));
            }
        }

        private int _deathSaveFailures;
        public int DeathSaveFailures
        {
            get => _deathSaveFailures;
            set
            {
                SetProperty(ref _deathSaveFailures, Math.Clamp(value, 0, 3));
                OnPropertyChanged(nameof(DeathSaveStatusText));
                OnPropertyChanged(nameof(IsStabilized));
                OnPropertyChanged(nameof(IsDead));
            }
        }

        public bool IsUnconscious => HP <= 0 && !IsDead && !IsStabilized;
        public bool IsStabilized => DeathSaveSuccesses >= 3;
        public bool IsDead => DeathSaveFailures >= 3;

        public string DeathSaveStatusText
        {
            get
            {
                if (HP > 0) return null;
                if (IsDead) return "💀 DEAD";
                if (IsStabilized) return "💤 Stabilized";
                return $"Death Saves: {DeathSaveSuccesses}✓ / {DeathSaveFailures}✗";
            }
        }

        public void ResetDeathSaves()
        {
            DeathSaveSuccesses = 0;
            DeathSaveFailures = 0;
        }

        public string RecordDeathSave(int roll)
        {
            if (roll == 20)
            {
                HP = 1;
                ResetDeathSaves();
                return $"☀️ Natural 20! {Name} regains 1 HP and is conscious!";
            }
            else if (roll == 1)
            {
                DeathSaveFailures += 2;
                if (IsDead)
                    return $"💀 Natural 1! {Name} suffers two failures and has died!";
                return $"😱 Natural 1! {Name} suffers two death save failures! ({DeathSaveSuccesses}✓ / {DeathSaveFailures}✗)";
            }
            else if (roll >= 10)
            {
                DeathSaveSuccesses++;
                if (IsStabilized)
                    return $"💤 {Name} succeeds and is now stabilized! ({DeathSaveSuccesses}✓ / {DeathSaveFailures}✗)";
                return $"✓ {Name} succeeds a death save ({DeathSaveSuccesses}✓ / {DeathSaveFailures}✗)";
            }
            else
            {
                DeathSaveFailures++;
                if (IsDead)
                    return $"💀 {Name} fails and has died! ({DeathSaveSuccesses}✓ / {DeathSaveFailures}✗)";
                return $"✗ {Name} fails a death save ({DeathSaveSuccesses}✓ / {DeathSaveFailures}✗)";
            }
        }

        public string TakeDamageWhileUnconscious(int damage, bool wasCritical)
        {
            if (wasCritical)
            {
                DeathSaveFailures += 2;
            }
            else
                DeathSaveFailures++;

            if (IsDead)
                return $"💀 {Name} takes damage while unconscious and has died!";

            string critText = wasCritical ? " (Critical - 2 failures!)" : "";
            return $"😵 {Name} takes damage while unconscious!{critText} ({DeathSaveSuccesses}✓ / {DeathSaveFailures}✗)";
        }

        #endregion

        #region Conditions
        public string ConditionsDisplay => Conditions.ToDisplayString();

        public void ToggleCondition(Condition condition)
        {
            if (HasCondition(condition))
                Conditions &= ~condition;
            else
                Conditions |= condition;
        }

        #endregion

        #region Spell Slots

        private SpellSlots _spellSlots;
        public SpellSlots SpellSlots
        {
            get => _spellSlots ??= new SpellSlots();
            set => SetProperty(ref _spellSlots, value);
        }

        public bool HasSpellSlots => SpellSlots?.HasSpellSlots ?? false;

        #endregion

        #region Notes

        private List<TokenNote> _combatNotes = new List<TokenNote>();
        public List<TokenNote> CombatNotes
        {
            get => _combatNotes;
            set => SetProperty(ref _combatNotes, value);
        }

        public void AddNote(string text, NoteType type = NoteType.General, int? expiresOnRound = null)
        {
            CombatNotes.Add(new TokenNote
            {
                Text = text,
                Type = type,
                ExpiresOnRound = expiresOnRound
            });
            OnPropertyChanged(nameof(CombatNotes));
            OnPropertyChanged(nameof(HasNotes));
        }

        public void RemoveNote(string noteId)
        {
            CombatNotes.RemoveAll(n => n.Id == noteId);
            OnPropertyChanged(nameof(CombatNotes));
            OnPropertyChanged(nameof(HasNotes));
        }

        public void ClearExpiredNotes(int currentRound)
        {
            CombatNotes.RemoveAll(n => n.ExpiresOnRound.HasValue && n.ExpiresOnRound <= currentRound);
            OnPropertyChanged(nameof(CombatNotes));
            OnPropertyChanged(nameof(HasNotes));
        }

        public bool HasNotes => CombatNotes?.Count > 0;

        #endregion

        // Helper Methods
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        public bool HasEncounterTag(string tag) => EncounterTags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public bool HasCondition(Condition condition) => Conditions.HasFlag(condition);

        public void AddCondition(Condition condition)
        {
            Conditions |= condition;
        }

        public void RemoveCondition(Condition condition)
        {
            Conditions &= ~condition;
        }

        public void ResetMovementForTurn()
        {
            
        }

    }
}
