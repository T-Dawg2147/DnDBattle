using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;
using Condition = DnDBattle.Models.Effects.Condition;

namespace DnDBattle.Models.Combat
{
    public class QuickAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public QuickActionType ActionType { get; set; }
        public string Parameter { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int SortOrder { get; set; }

        public Condition ConditionToToggle { get; set; }
        public string DiceExpression { get; set; }
        public string CustomCommand { get; set; }
    }

    public enum QuickActionType
    {
        ToggleCondition,
        RollDice,
        RollInitiative,
        RollSave,
        RollAbilityCheck,
        RollSavingThrow,
        ApplyCondition,
        Heal,
        Damage,
        AddTempHP,
        Custom
    }

    public static class QuickActionPresets
    {
        public static List<QuickAction> GetDefaultActions()
        {
            return new List<QuickAction>
            {
                // Combat Actions
                new QuickAction
                {
                    Id = "roll_initiative",
                    Name = "Roll Initiative",
                    Icon = "🎲",
                    Description = "Roll initiative for this creature",
                    ActionType = QuickActionType.RollInitiative,
                    IsEnabled = true,
                    SortOrder = 0
                },
                new QuickAction
                {
                    Id = "dodge",
                    Name = "Dodge",
                    Icon = "🏃",
                    Description = "Take the Dodge action (attacks against have disadvantage)",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Dodging,
                    IsEnabled = true,
                    SortOrder = 1
                },
                new QuickAction
                {
                    Id = "hide",
                    Name = "Hide",
                    Icon = "🥷",
                    Description = "Take the Hide action",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Hidden,
                    IsEnabled = true,
                    SortOrder = 2
                },
                new QuickAction
                {
                    Id = "concentrate",
                    Name = "Concentrate",
                    Icon = "🎯",
                    Description = "Mark as concentrating on a spell",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Concentrating,
                    IsEnabled = true,
                    SortOrder = 3
                },
                
                // Common Conditions
                new QuickAction
                {
                    Id = "prone",
                    Name = "Prone",
                    Icon = "🔽",
                    Description = "Toggle prone condition",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Prone,
                    IsEnabled = false,
                    SortOrder = 10
                },
                new QuickAction
                {
                    Id = "grappled",
                    Name = "Grappled",
                    Icon = "🤼",
                    Description = "Toggle grappled condition",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Grappled,
                    IsEnabled = false,
                    SortOrder = 11
                },
                new QuickAction
                {
                    Id = "restrained",
                    Name = "Restrained",
                    Icon = "⛓️",
                    Description = "Toggle restrained condition",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Restrained,
                    IsEnabled = false,
                    SortOrder = 12
                },
                new QuickAction
                {
                    Id = "stunned",
                    Name = "Stunned",
                    Icon = "💫",
                    Description = "Toggle stunned condition",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Stunned,
                    IsEnabled = false,
                    SortOrder = 13
                },
                new QuickAction
                {
                    Id = "frightened",
                    Name = "Frightened",
                    Icon = "😨",
                    Description = "Toggle frightened condition",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Frightened,
                    IsEnabled = false,
                    SortOrder = 14
                },
                new QuickAction
                {
                    Id = "poisoned",
                    Name = "Poisoned",
                    Icon = "☠️",
                    Description = "Toggle poisoned condition",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Poisoned,
                    IsEnabled = false,
                    SortOrder = 15
                },
                new QuickAction
                {
                    Id = "invisible",
                    Name = "Invisible",
                    Icon = "👻",
                    Description = "Toggle invisible condition",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Invisible,
                    IsEnabled = false,
                    SortOrder = 16
                },
                new QuickAction
                {
                    Id = "blinded",
                    Name = "Blinded",
                    Icon = "👁️‍🗨️",
                    Description = "Toggle blinded condition",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Blinded,
                    IsEnabled = false,
                    SortOrder = 17
                },
                
                // Buffs
                new QuickAction
                {
                    Id = "blessed",
                    Name = "Blessed",
                    Icon = "✨",
                    Description = "Toggle Bless spell effect (+1d4 to attacks/saves)",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Blessed,
                    IsEnabled = false,
                    SortOrder = 20
                },
                new QuickAction
                {
                    Id = "hasted",
                    Name = "Hasted",
                    Icon = "⚡",
                    Description = "Toggle Haste spell effect",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Hasted,
                    IsEnabled = false,
                    SortOrder = 21
                },
                new QuickAction
                {
                    Id = "flying",
                    Name = "Flying",
                    Icon = "🦅",
                    Description = "Toggle flying status",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Flying,
                    IsEnabled = false,
                    SortOrder = 22
                },
                new QuickAction
                {
                    Id = "raging",
                    Name = "Raging",
                    Icon = "😤",
                    Description = "Toggle Barbarian Rage",
                    ActionType = QuickActionType.ToggleCondition,
                    ConditionToToggle = Condition.Raging,
                    IsEnabled = false,
                    SortOrder = 23
                },
                
                // Saving Throws
                new QuickAction
                {
                    Id = "roll_str_save",
                    Name = "STR Save",
                    Icon = "💪",
                    Description = "Roll a Strength saving throw",
                    ActionType = QuickActionType.RollSave,
                    CustomCommand = "STR",
                    IsEnabled = false,
                    SortOrder = 30
                },
                new QuickAction
                {
                    Id = "roll_dex_save",
                    Name = "DEX Save",
                    Icon = "🏹",
                    Description = "Roll a Dexterity saving throw",
                    ActionType = QuickActionType.RollSave,
                    CustomCommand = "DEX",
                    IsEnabled = false,
                    SortOrder = 31
                },
                new QuickAction
                {
                    Id = "roll_con_save",
                    Name = "CON Save",
                    Icon = "🛡️",
                    Description = "Roll a Constitution saving throw",
                    ActionType = QuickActionType.RollSave,
                    CustomCommand = "CON",
                    IsEnabled = false,
                    SortOrder = 32
                },
                new QuickAction
                {
                    Id = "roll_wis_save",
                    Name = "WIS Save",
                    Icon = "👁️",
                    Description = "Roll a Wisdom saving throw",
                    ActionType = QuickActionType.RollSave,
                    CustomCommand = "WIS",
                    IsEnabled = false,
                    SortOrder = 33
                },
                new QuickAction
                {
                    Id = "roll_int_save",
                    Name = "INT Save",
                    Icon = "🧠",
                    Description = "Roll an Intelligence saving throw",
                    ActionType = QuickActionType.RollSave,
                    CustomCommand = "INT",
                    IsEnabled = false,
                    SortOrder = 34
                },
                new QuickAction
                {
                    Id = "roll_cha_save",
                    Name = "CHA Save",
                    Icon = "💬",
                    Description = "Roll a Charisma saving throw",
                    ActionType = QuickActionType.RollSave,
                    CustomCommand = "CHA",
                    IsEnabled = false,
                    SortOrder = 35
                },
                
                // Common Rolls
                new QuickAction
                {
                    Id = "roll_perception",
                    Name = "Perception",
                    Icon = "👀",
                    Description = "Roll a Perception check",
                    ActionType = QuickActionType.RollAbilityCheck,
                    CustomCommand = "WIS:Perception",
                    IsEnabled = false,
                    SortOrder = 40
                },
                new QuickAction
                {
                    Id = "roll_stealth",
                    Name = "Stealth",
                    Icon = "🤫",
                    Description = "Roll a Stealth check",
                    ActionType = QuickActionType.RollAbilityCheck,
                    CustomCommand = "DEX:Stealth",
                    IsEnabled = false,
                    SortOrder = 41
                },
                new QuickAction
                {
                    Id = "roll_athletics",
                    Name = "Athletics",
                    Icon = "🏋️",
                    Description = "Roll an Athletics check",
                    ActionType = QuickActionType.RollAbilityCheck,
                    CustomCommand = "STR:Athletics",
                    IsEnabled = false,
                    SortOrder = 42
                },
                new QuickAction
                {
                    Id = "roll_acrobatics",
                    Name = "Acrobatics",
                    Icon = "🤸",
                    Description = "Roll an Acrobatics check",
                    ActionType = QuickActionType.RollAbilityCheck,
                    CustomCommand = "DEX:Acrobatics",
                    IsEnabled = false,
                    SortOrder = 43
                },
                
                // Concentration Check
                new QuickAction
                {
                    Id = "concentration_check",
                    Name = "Concentration",
                    Icon = "🎯",
                    Description = "Roll a Concentration check (CON save)",
                    ActionType = QuickActionType.RollSave,
                    CustomCommand = "CON",
                    IsEnabled = false,
                    SortOrder = 50
                },
                
                // Death Saves
                new QuickAction
                {
                    Id = "death_save",
                    Name = "Death Save",
                    Icon = "💀",
                    Description = "Roll a death saving throw",
                    ActionType = QuickActionType.RollDice,
                    DiceExpression = "1d20",
                    IsEnabled = false,
                    SortOrder = 51
                }
            };
        }
    }
}
