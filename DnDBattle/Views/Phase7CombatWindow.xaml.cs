using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DnDBattle.Controls;
using DnDBattle.Models;
using DnDBattle.Services;
using DnDBattle.ViewModels;

namespace DnDBattle.Views
{
    /// <summary>
    /// Phase 7: Combat Automation window.
    /// Provides UI for attack rolls, saving throws, spell slot tracking,
    /// concentration checks, condition automation, and cover calculation.
    /// </summary>
    public partial class Phase7CombatWindow : Window
    {
        // Reference to the battle grid for accessing tokens, walls, and grid state
        private readonly BattleGridControl _battleGrid;

        // Reference to the main view model for selected token and action log
        private readonly MainViewModel _viewModel;

        // Service instances for combat automation
        private readonly AttackRollSystem _attackRollSystem = new AttackRollSystem();
        private readonly SavingThrowSystem _savingThrowSystem = new SavingThrowSystem();
        private readonly ConcentrationService _concentrationService = new ConcentrationService();
        private readonly CoverSystem _coverSystem = new CoverSystem();
        private readonly SpellCastingService _spellCastingService;

        public Phase7CombatWindow(BattleGridControl battleGrid, MainViewModel viewModel)
        {
            InitializeComponent();
            _battleGrid = battleGrid;
            _viewModel = viewModel;
            _spellCastingService = new SpellCastingService(_concentrationService);

            LoadCurrentValues();
            PopulateTokenLists();

            // Update selected token display whenever the window is activated
            Activated += (s, e) => RefreshSelectedToken();
        }

        /// <summary>
        /// Load current option values into all toggle controls
        /// </summary>
        private void LoadCurrentValues()
        {
            // 7.1 Attack Roll System
            ChkEnableAttackRolls.IsChecked = Options.EnableAttackRollSystem;
            ChkAutoApplyDamage.IsChecked = Options.AutoApplyDamage;

            // 7.2 Saving Throw Automation
            ChkEnableSavingThrows.IsChecked = Options.EnableSavingThrowAutomation;
            ChkAutoRollMonsterSaves.IsChecked = Options.AutoRollMonsterSaves;

            // 7.3 Spell Slot Tracking
            ChkEnableSpellSlots.IsChecked = Options.EnableSpellSlotTracking;

            // 7.4 Concentration Tracking
            ChkEnableConcentration.IsChecked = Options.EnableConcentrationTracking;
            ChkAutoPromptConcentration.IsChecked = Options.AutoPromptConcentrationCheck;

            // 7.5 Condition Automation
            ChkEnableConditions.IsChecked = Options.EnableConditionAutomation;

            // 7.6 Cover System
            ChkEnableCover.IsChecked = Options.EnableCoverSystem;
        }

        /// <summary>
        /// Populate target token combo boxes from the battle grid's token collection
        /// </summary>
        private void PopulateTokenLists()
        {
            var tokens = _viewModel.Tokens;
            if (tokens != null)
            {
                CmbAttackTarget.ItemsSource = tokens;
            }
        }

        /// <summary>
        /// Update the selected token display and concentration status
        /// </summary>
        private void RefreshSelectedToken()
        {
            var token = _viewModel.SelectedToken;
            if (token != null)
            {
                TxtSelectedToken.Text = $"Selected Token: {token.Name} (HP: {token.HP}/{token.MaxHP}, AC: {token.ArmorClass})";
                UpdateConcentrationStatus(token);
                UpdateSlotDisplay(token);
            }
            else
            {
                TxtSelectedToken.Text = "Selected Token: (none)";
            }
        }

        #region 7.1 Attack Roll System

        /// <summary>
        /// Rolls an attack from the selected token against the chosen target using form values.
        /// Creates an ad-hoc Action with the specified bonus and damage expression.
        /// </summary>
        private void RollAttack_Click(object sender, RoutedEventArgs e)
        {
            var attacker = _viewModel.SelectedToken;
            if (attacker == null)
            {
                MessageBox.Show("Select an attacker token first.", "Attack Roll", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var defender = CmbAttackTarget.SelectedItem as Token;
            if (defender == null)
            {
                MessageBox.Show("Select a target token.", "Attack Roll", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtAttackBonus.Text, out int attackBonus))
            {
                MessageBox.Show("Enter a valid attack bonus number.", "Attack Roll", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Build an ad-hoc attack action from the form fields
            var attack = new Models.Action
            {
                Name = "Manual Attack",
                AttackBonus = attackBonus,
                DamageExpression = TxtDamageExpression.Text
            };

            // Parse the selected attack mode
            var mode = (AttackMode)CmbAttackMode.SelectedIndex;

            // Optionally calculate cover between attacker and defender
            var cover = CoverLevel.None;
            if (Options.EnableCoverSystem)
            {
                cover = _coverSystem.CalculateCover(
                    attacker, defender,
                    isBlocked: (x, y) => !_battleGrid.WallService.HasLineOfSight(
                        new System.Windows.Point(attacker.GridX, attacker.GridY),
                        new System.Windows.Point(x, y)));
            }

            var result = _attackRollSystem.RollAttack(attacker, defender, attack, mode, cover);

            // Format and display the result
            string resultText = $"🎲 {attacker.Name} attacks {defender.Name}!\n" +
                                $"   d20: {result.D20Roll} + {result.AttackBonus} = {result.TotalAttack} vs AC {result.TargetAC}\n";

            if (result.IsCriticalHit) resultText += "   💥 CRITICAL HIT!\n";
            else if (result.IsCriticalFumble) resultText += "   ❌ CRITICAL FUMBLE!\n";

            if (result.Hit)
                resultText += $"   ✅ HIT! Damage: {result.DamageRoll} → {result.ActualDamage} dealt. {result.DamageDescription}";
            else
                resultText += "   ❌ MISS!";

            if (result.Cover != CoverLevel.None)
                resultText += $"\n   🧱 Cover: {result.Cover}";

            TxtAttackResult.Text = resultText;

            // Log the attack to the action log
            _viewModel.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = $"Attack: {attacker.Name} → {defender.Name}: " +
                          (result.Hit ? $"HIT for {result.ActualDamage} damage" : "MISS") +
                          (result.IsCriticalHit ? " (CRIT)" : "")
            });

            // Refresh token display after potential damage
            RefreshSelectedToken();
        }

        #endregion

        #region 7.2 Saving Throw Automation

        /// <summary>
        /// Rolls a saving throw for the selected token using form values.
        /// Displays the full result including modifiers and success/failure.
        /// </summary>
        private void RollSave_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel.SelectedToken;
            if (token == null)
            {
                MessageBox.Show("Select a token to roll a save for.", "Saving Throw", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TxtSaveDC.Text, out int dc))
            {
                MessageBox.Show("Enter a valid DC number.", "Saving Throw", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ability = (Ability)CmbSaveAbility.SelectedIndex;
            var result = _savingThrowSystem.RollSave(token, ability, dc);

            // Format and display the result
            string resultText = $"🛡️ {token.Name} {ability} Save (DC {dc}):\n" +
                                $"   d20: {result.D20Roll} + {result.Modifier} = {result.Total}\n";

            if (result.AutoFailed) resultText += "   ⚠️ AUTO-FAILED (condition)\n";
            if (result.IsNaturalTwenty) resultText += "   🌟 Natural 20!\n";
            if (result.IsNaturalOne) resultText += "   💀 Natural 1!\n";
            if (result.UsedLegendaryResistance) resultText += "   👑 Used Legendary Resistance!\n";

            resultText += result.Success ? "   ✅ SUCCESS!" : "   ❌ FAILURE!";

            TxtSaveResult.Text = resultText;

            // Log the save result
            _viewModel.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = $"Save: {token.Name} {ability} DC {dc}: {(result.Success ? "SUCCESS" : "FAIL")} (rolled {result.Total})"
            });
        }

        #endregion

        #region 7.3 Spell Slot Tracking

        /// <summary>
        /// Initializes spell slots for the selected token based on caster level and type.
        /// Uses the SpellSlots.GetForCasterLevel factory method.
        /// </summary>
        private void InitializeSlots_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel.SelectedToken;
            if (token == null)
            {
                MessageBox.Show("Select a token first.", "Spell Slots", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int casterLevel = CmbCasterLevel.SelectedIndex + 1;
            var casterType = (CasterType)CmbCasterType.SelectedIndex;

            // Initialize slots from the 5e standard progression table
            token.SpellSlots = SpellSlots.GetForCasterLevel(casterLevel, casterType);
            UpdateSlotDisplay(token);

            _viewModel.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = $"Spell slots initialized for {token.Name}: Level {casterLevel} {casterType} caster"
            });
        }

        /// <summary>
        /// Updates the slot display text showing current/max for each spell level
        /// </summary>
        private void UpdateSlotDisplay(Token token)
        {
            if (token?.SpellSlots == null || !token.SpellSlots.HasSpellSlots)
            {
                TxtSlotDisplay.Text = "No slots initialized.";
                return;
            }

            var parts = new System.Collections.Generic.List<string>();
            for (int i = 1; i <= 9; i++)
            {
                var display = token.SpellSlots.GetDisplayForLevel(i);
                if (display != null)
                    parts.Add($"Lvl {i}: {display}");
            }
            TxtSlotDisplay.Text = string.Join("  |  ", parts);
        }

        /// <summary>
        /// Uses a spell slot of the selected level from the selected token
        /// </summary>
        private void UseSlot_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel.SelectedToken;
            if (token?.SpellSlots == null)
            {
                MessageBox.Show("Select a token with initialized spell slots.", "Spell Slots", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int level = CmbSlotLevel.SelectedIndex + 1;
            if (token.SpellSlots.UseSlot(level))
            {
                UpdateSlotDisplay(token);
                _viewModel.ActionLog.Insert(0, new ActionLogEntry
                {
                    Source = "Phase7",
                    Message = $"{token.Name} used a level {level} spell slot."
                });
            }
            else
            {
                MessageBox.Show($"No level {level} slots remaining!", "Spell Slots", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Restores a spell slot of the selected level for the selected token
        /// </summary>
        private void RestoreSlot_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel.SelectedToken;
            if (token?.SpellSlots == null)
            {
                MessageBox.Show("Select a token with initialized spell slots.", "Spell Slots", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int level = CmbSlotLevel.SelectedIndex + 1;
            token.SpellSlots.RestoreSlot(level);
            UpdateSlotDisplay(token);
        }

        /// <summary>
        /// Performs a long rest, restoring all spell slots to maximum
        /// </summary>
        private void LongRest_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel.SelectedToken;
            if (token?.SpellSlots == null)
            {
                MessageBox.Show("Select a token with initialized spell slots.", "Spell Slots", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            token.SpellSlots.LongRest();
            UpdateSlotDisplay(token);

            _viewModel.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = $"{token.Name} completed a long rest. All spell slots restored."
            });
        }

        /// <summary>
        /// Performs a short rest (Warlock pact slots would restore here)
        /// </summary>
        private void ShortRest_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel.SelectedToken;
            if (token?.SpellSlots == null)
            {
                MessageBox.Show("Select a token with initialized spell slots.", "Spell Slots", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            token.SpellSlots.ShortRest();
            UpdateSlotDisplay(token);

            _viewModel.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = $"{token.Name} completed a short rest."
            });
        }

        #endregion

        #region 7.4 Concentration Tracking

        /// <summary>
        /// Updates the concentration status display for a token
        /// </summary>
        private void UpdateConcentrationStatus(Token token)
        {
            if (token.IsConcentrating)
                TxtConcentrationStatus.Text = $"🔮 Concentrating on: {token.ConcentrationSpell}";
            else
                TxtConcentrationStatus.Text = "Not concentrating.";
        }

        /// <summary>
        /// Starts concentration on a spell, breaking any previous concentration
        /// </summary>
        private void StartConcentration_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel.SelectedToken;
            if (token == null)
            {
                MessageBox.Show("Select a token first.", "Concentration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string spellName = TxtConcentrationSpell.Text?.Trim();
            if (string.IsNullOrEmpty(spellName))
            {
                MessageBox.Show("Enter a spell name.", "Concentration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _concentrationService.StartConcentration(token, spellName);
            UpdateConcentrationStatus(token);

            _viewModel.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = $"{token.Name} began concentrating on {spellName}."
            });
        }

        /// <summary>
        /// Checks concentration after taking damage. DC = max(10, damage/2).
        /// Automatically breaks concentration on failure.
        /// </summary>
        private void CheckConcentration_Click(object sender, RoutedEventArgs e)
        {
            var token = _viewModel.SelectedToken;
            if (token == null)
            {
                MessageBox.Show("Select a token first.", "Concentration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!token.IsConcentrating)
            {
                MessageBox.Show($"{token.Name} is not concentrating on any spell.", "Concentration", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(TxtConcentrationDamage.Text, out int damage) || damage < 0)
            {
                MessageBox.Show("Enter a valid damage amount.", "Concentration", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = _concentrationService.CheckConcentration(token, damage);

            TxtConcentrationResult.Text = result.ToString();
            UpdateConcentrationStatus(token);

            // Log the concentration check
            _viewModel.ActionLog.Insert(0, new ActionLogEntry
            {
                Source = "Phase7",
                Message = result.ToString()
            });
        }

        #endregion

        #region 7.6 Cover System

        /// <summary>
        /// Calculates cover between two tokens or manual positions using the WallService.
        /// Uses Bresenham line to check for blocking walls along the path.
        /// </summary>
        private void CalculateCover_Click(object sender, RoutedEventArgs e)
        {
            Token attacker = null;
            Token defender = null;

            if (ChkUseSelectedTokens.IsChecked == true)
            {
                // Use the selected token as attacker and the attack target as defender
                attacker = _viewModel.SelectedToken;
                defender = CmbAttackTarget.SelectedItem as Token;

                if (attacker == null || defender == null)
                {
                    MessageBox.Show("Select an attacker (selected token) and a defender (target combo) to calculate cover.",
                        "Cover System", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                // Use manual position inputs - create temporary tokens for the calculation
                if (!int.TryParse(TxtAttackerX.Text, out int ax) || !int.TryParse(TxtAttackerY.Text, out int ay) ||
                    !int.TryParse(TxtDefenderX.Text, out int dx) || !int.TryParse(TxtDefenderY.Text, out int dy))
                {
                    MessageBox.Show("Enter valid X,Y coordinates.", "Cover System", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                attacker = new Token { Name = "Attacker", GridX = ax, GridY = ay };
                defender = new Token { Name = "Defender", GridX = dx, GridY = dy };
            }

            // Calculate cover using the WallService for line-of-sight blocking
            var wallService = _battleGrid.WallService;
            var coverLevel = _coverSystem.CalculateCover(
                attacker, defender,
                isBlocked: (x, y) => !wallService.HasLineOfSight(
                    new System.Windows.Point(attacker.GridX, attacker.GridY),
                    new System.Windows.Point(x, y)));

            int effectiveAC = CoverSystem.GetEffectiveAC(defender, coverLevel);

            string coverText = coverLevel switch
            {
                CoverLevel.None => "No Cover",
                CoverLevel.Half => "Half Cover (+2 AC, +2 DEX saves)",
                CoverLevel.ThreeQuarters => "Three-Quarters Cover (+5 AC, +5 DEX saves)",
                CoverLevel.Full => "Full Cover (cannot be targeted directly)",
                _ => "Unknown"
            };

            TxtCoverResult.Text = $"🧱 {attacker.Name} → {defender.Name}: {coverText}\n" +
                                  $"   Effective AC: {effectiveAC}";
        }

        #endregion

        #region Feature Toggles - Sync checkboxes to Options

        /// <summary>
        /// Syncs all feature toggle checkboxes to the static Options properties
        /// </summary>
        private void OnFeatureToggled(object sender, RoutedEventArgs e)
        {
            // 7.1 Attack Roll System
            Options.EnableAttackRollSystem = ChkEnableAttackRolls.IsChecked == true;
            Options.AutoApplyDamage = ChkAutoApplyDamage.IsChecked == true;

            // 7.2 Saving Throw Automation
            Options.EnableSavingThrowAutomation = ChkEnableSavingThrows.IsChecked == true;
            Options.AutoRollMonsterSaves = ChkAutoRollMonsterSaves.IsChecked == true;

            // 7.3 Spell Slot Tracking
            Options.EnableSpellSlotTracking = ChkEnableSpellSlots.IsChecked == true;

            // 7.4 Concentration Tracking
            Options.EnableConcentrationTracking = ChkEnableConcentration.IsChecked == true;
            Options.AutoPromptConcentrationCheck = ChkAutoPromptConcentration.IsChecked == true;

            // 7.5 Condition Automation
            Options.EnableConditionAutomation = ChkEnableConditions.IsChecked == true;

            // 7.6 Cover System
            Options.EnableCoverSystem = ChkEnableCover.IsChecked == true;
        }

        #endregion
    }
}
