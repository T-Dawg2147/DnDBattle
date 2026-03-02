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
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;

namespace DnDBattle.Views.TileMap
{
    public partial class MapResizeDialog : Window
    {
        public int NewWidth { get; private set; }
        public int NewHeight { get; private set; }

        private readonly Models.Tiles.TileMap _map;

        public MapResizeDialog(Models.Tiles.TileMap map)
        {
            InitializeComponent();
            _map = map;

            TxtCurrentSize.Text = $"{map.Width} × {map.Height}";
            TxtTileCount.Text = $"Contains {map.PlacedTiles.Count} tiles";

            TxtWidth.Text = map.Width.ToString();
            TxtHeight.Text = map.Height.ToString();

            UpdatePreview();
        }

        private void Size_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (!int.TryParse(TxtWidth.Text, out int width) || !int.TryParse(TxtHeight.Text, out int height))
            {
                TxtPreview.Text = "❌ Invalid dimensions";
                return;
            }

            if (width < 10 || height < 10)
            {
                TxtPreview.Text = "❌ Minimum size is 10×10";
                return;
            }

            if (width > 200 || height > 200)
            {
                TxtPreview.Text = "⚠️ Large maps may impact performance";
            }

            // Check how many tiles will be lost
            int tilesOutsideBounds = _map.PlacedTiles.Count(t => t.GridX >= width || t.GridY >= height);

            if (tilesOutsideBounds > 0)
            {
                TxtPreview.Text = $"⚠️ {tilesOutsideBounds} tiles will be removed";
            }
            else if (width > _map.Width || height > _map.Height)
            {
                TxtPreview.Text = "✅ Expanding map - no tiles will be lost";
            }
            else
            {
                TxtPreview.Text = "✅ No tiles will be affected";
            }
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtWidth.Text, out int width) || !int.TryParse(TxtHeight.Text, out int height))
            {
                MessageBox.Show("Please enter valid dimensions.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (width < 10 || height < 10)
            {
                MessageBox.Show("Minimum map size is 10×10.", "Invalid Size",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (width > 200 || height > 200)
            {
                var result = MessageBox.Show(
                    "Creating very large maps may impact performance. Continue?",
                    "Large Map Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Check for tile loss
            int tilesOutsideBounds = _map.PlacedTiles.Count(t => t.GridX >= width || t.GridY >= height);
            if (tilesOutsideBounds > 0)
            {
                var result = MessageBox.Show(
                    $"Resizing will remove {tilesOutsideBounds} tiles outside the new boundaries.\n\nThis cannot be undone. Continue?",
                    "Confirm Resize",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            NewWidth = width;
            NewHeight = height;
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
