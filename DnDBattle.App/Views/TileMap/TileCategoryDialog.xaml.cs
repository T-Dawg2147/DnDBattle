using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;

namespace DnDBattle.Views.TileMap
{
    public partial class TileCategoryDialog : Window
    {
        public string SelectedCategory { get; private set; }

        public TileCategoryDialog(List<string> existingCategories)
        {
            InitializeComponent();

            CmbCategory.ItemsSource = existingCategories;
            CmbCategory.SelectedIndex = existingCategories.IndexOf("Custom");
        }

        private void TxtNewCategory_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // If user types in new category, deselect combo
            if (!string.IsNullOrWhiteSpace(TxtNewCategory.Text))
            {
                CmbCategory.SelectedIndex = -1;
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            // Check if using new category or existing
            if (!string.IsNullOrWhiteSpace(TxtNewCategory.Text))
            {
                SelectedCategory = TxtNewCategory.Text.Trim();
            }
            else if (CmbCategory.SelectedItem is string category)
            {
                SelectedCategory = category;
            }
            else
            {
                MessageBox.Show("Please select or enter a category.", "Category Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}