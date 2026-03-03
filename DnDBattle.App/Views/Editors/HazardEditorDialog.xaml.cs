using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;

namespace DnDBattle.Views.Editors
{
    public partial class HazardEditorDialog : Window
    {
        public HazardMetadata Hazard { get; private set; }

        public HazardEditorDialog(HazardMetadata existing = null)
        {
            InitializeComponent();
            Hazard = existing ?? new HazardMetadata();

            if (existing != null)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            TxtName.Text = Hazard.Name;
            CmbHazardType.SelectedIndex = (int)Hazard.HazardKind;
            TxtDescription.Text = Hazard.Description;
            TxtDamageDice.Text = Hazard.DamageDice;
            CmbDamageType.SelectedIndex = GetDamageTypeIndex(Hazard.DamageType);
            CmbDamageTrigger.SelectedIndex = (int)Hazard.DamageTrigger;
            ChkAllowsSave.IsChecked = Hazard.AllowsSave;
            CmbSaveAbility.SelectedIndex = GetSaveAbilityIndex(Hazard.SaveAbility);
            TxtSaveDC.Text = Hazard.SaveDC.ToString();
            ChkSaveNegates.IsChecked = Hazard.SaveNegatesDamage;
            ChkSaveHalves.IsChecked = Hazard.SaveHalvesDamage;
            ChkDamagesEachTurn.IsChecked = Hazard.DamagesEachTurn;
            TxtPerTurnDamage.Text = Hazard.PerTurnDamage;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Hazard.Name = TxtName.Text;
            Hazard.HazardKind = (HazardType)CmbHazardType.SelectedIndex;
            Hazard.Description = TxtDescription.Text;
            Hazard.DamageDice = TxtDamageDice.Text;
            Hazard.DamageType = GetDamageTypeFromIndex(CmbDamageType.SelectedIndex);
            Hazard.DamageTrigger = (HazardTrigger)CmbDamageTrigger.SelectedIndex;
            Hazard.AllowsSave = ChkAllowsSave.IsChecked ?? true;
            Hazard.SaveAbility = GetSaveAbilityFromIndex(CmbSaveAbility.SelectedIndex);
            Hazard.SaveDC = int.TryParse(TxtSaveDC.Text, out int dc) ? dc : 13;
            Hazard.SaveNegatesDamage = ChkSaveNegates.IsChecked ?? false;
            Hazard.SaveHalvesDamage = ChkSaveHalves.IsChecked ?? true;
            Hazard.DamagesEachTurn = ChkDamagesEachTurn.IsChecked ?? false;
            Hazard.PerTurnDamage = TxtPerTurnDamage.Text;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private int GetDamageTypeIndex(DamageType type)
        {
            return type switch
            {
                DamageType.Bludgeoning => 0,
                DamageType.Piercing => 1,
                DamageType.Slashing => 2,
                DamageType.Fire => 3,
                DamageType.Cold => 4,
                DamageType.Lightning => 5,
                DamageType.Thunder => 6,
                DamageType.Acid => 7,
                DamageType.Poison => 8,
                DamageType.Necrotic => 9,
                DamageType.Radiant => 10,
                DamageType.Force => 11,
                DamageType.Psychic => 12,
                _ => 3
            };
        }

        private DamageType GetDamageTypeFromIndex(int index)
        {
            return index switch
            {
                0 => DamageType.Bludgeoning,
                1 => DamageType.Piercing,
                2 => DamageType.Slashing,
                3 => DamageType.Fire,
                4 => DamageType.Cold,
                5 => DamageType.Lightning,
                6 => DamageType.Thunder,
                7 => DamageType.Acid,
                8 => DamageType.Poison,
                9 => DamageType.Necrotic,
                10 => DamageType.Radiant,
                11 => DamageType.Force,
                12 => DamageType.Psychic,
                _ => DamageType.Fire
            };
        }

        private int GetSaveAbilityIndex(string ability)
        {
            return ability.ToUpper() switch
            {
                "STR" => 0,
                "DEX" => 1,
                "CON" => 2,
                "INT" => 3,
                "WIS" => 4,
                "CHA" => 5,
                _ => 1
            };
        }

        private string GetSaveAbilityFromIndex(int index)
        {
            return index switch
            {
                0 => "STR",
                1 => "DEX",
                2 => "CON",
                3 => "INT",
                4 => "WIS",
                5 => "CHA",
                _ => "DEX"
            };
        }
    }
}
