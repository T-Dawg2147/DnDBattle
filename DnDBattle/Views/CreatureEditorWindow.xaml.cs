using DnDBattle.Models;
using DnDBattle.Services;
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

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for CreatureEditorWindow.xaml
    /// </summary>
    public partial class CreatureEditorWindow : Window
    {
        private Token _creature;
        private bool _isNewCreature;
        private List<CreatureCategory> _categories;

        public Token ResultCreature { get; private set; }

        public CreatureEditorWindow(Token creature = null)
        {
            InitializeComponent();

            _isNewCreature = creature == null;
            _creature = creature ?? new Token { Id = Guid.NewGuid() };

            Loaded += CreatureEditorWindow_Loaded;
        }

        private async void CreatureEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            using (var db = new CreatureDatabaseService())
            {
                _categories = await db.GetCategoriesAsync();
            }

            CmbCategory.Items.Clear();
            foreach (var cat in _categories.Where(c => c.Id != "favorites"))
            {
                CmbCategory.Items.Add(new ComboBoxItem()
                {
                    Content = $"{cat.Icon} {cat.Name}",
                    Tag = cat.Id
                });
            }

            var customItem = CmbCategory.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == "custom");
            if (customItem != null)
                CmbCategory.SelectedItem = customItem;

            if (!_isNewCreature)
            {
                Title = $"Edit Creature - {_creature.Name}";

                TxtName.Text = _creature.Name;
                CmbType.Text = _creature.Type;
                CmbSize.Text = _creature.Size;
                CmbAlignment.Text = _creature.Alignment;
                TxtAC.Text = _creature.ArmorClass.ToString();
                TxtHP.Text = _creature.MaxHP.ToString();
                TxtHitDice.Text = _creature.HitDice;
                TxtCR.Text = _creature.ChallengeRating;
                TxtSpeed.Text = _creature.Speed;
                TxtInitMod.Text = _creature.InitiativeModifier.ToString();
                TxtStr.Text = _creature.Str.ToString();
                TxtDex.Text = _creature.Dex.ToString();
                TxtCon.Text = _creature.Con.ToString();
                TxtInt.Text = _creature.Int.ToString();
                TxtWis.Text = _creature.Wis.ToString();
                TxtCha.Text = _creature.Cha.ToString();
                TxtTraits.Text = _creature.Traits;
            }
            else
            {
                Title = "Create New Creature";
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Please enter a creature name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _creature.Name = TxtName.Text.Trim();
            _creature.Type = CmbType.Text;
            _creature.Size = (CmbSize.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Medium";
            _creature.Alignment = CmbAlignment.Text;

            int.TryParse(TxtAC.Text, out int ac);
            _creature.ArmorClass = ac;

            int.TryParse(TxtHP.Text, out int hp);
            _creature.MaxHP = hp;
            _creature.HP = hp;

            _creature.HitDice = TxtHitDice.Text;
            _creature.ChallengeRating = TxtCR.Text;
            _creature.Speed = TxtSpeed.Text;

            int.TryParse(TxtInitMod.Text, out int initMod);
            _creature.InitiativeModifier = initMod;

            int.TryParse(TxtStr.Text, out int str);
            _creature.Str = str > 0 ? str : 10;
            int.TryParse(TxtDex.Text, out int dex);
            _creature.Dex = dex > 0 ? dex : 10;
            int.TryParse(TxtCon.Text, out int con);
            _creature.Con = con > 0 ? con : 10;
            int.TryParse(TxtInt.Text, out int intVal);
            _creature.Int = intVal > 0 ? intVal : 10;
            int.TryParse(TxtWis.Text, out int wis);
            _creature.Wis = wis > 0 ? wis : 10;
            int.TryParse(TxtCha.Text, out int cha);
            _creature.Cha = cha > 0 ? cha : 10;

            _creature.Traits = TxtTraits.Text;

            var categoryId = (CmbCategory.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            try
            {
                using (var db = new CreatureDatabaseService())
                {
                    if (_isNewCreature)
                        await db.AddCustomCreatureAsync(_creature, categoryId);
                    else
                        await db.UpdateCreatureAsync(_creature);
                }

                ResultCreature = _creature;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving creature: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
