using DnDBattle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
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
using DnDBattle.Models.Tiles;

namespace DnDBattle.Views.Effects
{
    /// <summary>
    /// Interaction logic for AreaEffectToolbar.xaml
    /// </summary>
    public partial class AreaEffectToolbar : UserControl
    {
        public event Action<AreaEffectShape> ShapeSelected;
        public event Action<int> SizeChanged;
        public event Action<Color> ColorChanged;
        public event Action<AreaEffect> PresetSelected;
        public event System.Action CancelRequested;
        public event System.Action ClearAllRequested;

        private AreaEffectShape? _currentShape;
        private int _currentSize = 20;
        private Color _currentColor = Color.FromArgb(120, 255, 96, 0);

        public AreaEffectShape? CurrentShape => _currentShape;
        public int CurrentSize => _currentSize;
        public Color CurrentColor => _currentColor;

        public AreaEffectToolbar()
        {
            InitializeComponent();
        }

        private void ShapeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn)
            {
                BtnSphere.IsChecked = btn == BtnSphere && btn.IsChecked == true;
                BtnCube.IsChecked = btn == BtnCube && btn.IsChecked == true;
                BtnCone.IsChecked = btn == BtnCone && btn.IsChecked == true;
                BtnLine.IsChecked = btn == BtnLine && btn.IsChecked == true;

                if (btn.IsChecked == true && btn.Tag is string shapeStr)
                {
                    _currentShape = Enum.Parse<AreaEffectShape>(shapeStr);
                    BtnCancel.Visibility = Visibility.Visible;
                    ShapeSelected?.Invoke(_currentShape.Value);
                }
                else
                {
                    _currentShape = null;
                    BtnCancel.Visibility = Visibility.Collapsed;
                    CancelRequested?.Invoke();
                }
            }
        }

        private void CmbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbSize.SelectedItem is ComboBoxItem item)
            {
                string sizeText = item.Content.ToString().Replace(" ft", "");
                if (int.TryParse(sizeText, out int size))
                {
                    _currentSize = size;
                    SizeChanged?.Invoke(size);
                }
            }
        }

        private void CmbColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentColor = CmbColor.SelectedIndex switch
            {
                0 => Color.FromArgb(120, 255, 69, 0),
                1 => Color.FromArgb(120, 135, 206, 235),
                2 => Color.FromArgb(120, 147, 112, 219),
                3 => Color.FromArgb(120, 50, 205, 50),
                4 => Color.FromArgb(120, 255, 215, 0),
                5 => Color.FromArgb(120, 47, 79, 79),
                _ => Color.FromArgb(120, 255, 69, 0)
            };
            ColorChanged?.Invoke(_currentColor);
        }

        private void BtnPresets_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();

            var presets = AreaEffectPresets.GetAllPresets();

            foreach (var preset in presets)
            {
                var item = new MenuItem()
                {
                    Header = $"{GetShapeIcon(preset.Shape)} {preset.Name} ({preset.SizeInFeet} ft)",
                    Tag = preset
                };
                item.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is AreaEffect effect)
                    {
                        var copy = new AreaEffect()
                        {
                            Name = effect.Name,
                            Shape = effect.Shape,
                            SizeInFeet = effect.SizeInFeet,
                            WidthInFeet = effect.WidthInFeet,
                            Color = effect.Color
                        };
                        PresetSelected?.Invoke(copy);

                        SelectShapeButton(copy.Shape);
                        SelectSize(copy.SizeInFeet);
                    }
                };
                menu.Items.Add(item);
            }

            menu.PlacementTarget = BtnPresets;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private string GetShapeIcon(AreaEffectShape shape)
        {
            return shape switch
            {
                AreaEffectShape.Sphere => "⭕",
                AreaEffectShape.Cylinder => "⭕",
                AreaEffectShape.Cube => "⬜",
                AreaEffectShape.Square => "⬜",
                AreaEffectShape.Cone => "🔺",
                AreaEffectShape.Line => "➖",
                _ => "⭕"
            };
        }

        private void SelectShapeButton(AreaEffectShape shape)
        {
            BtnSphere.IsChecked = shape == AreaEffectShape.Sphere || shape == AreaEffectShape.Cylinder;
            BtnCube.IsChecked = shape == AreaEffectShape.Cube || shape == AreaEffectShape.Square;
            BtnCone.IsChecked = shape == AreaEffectShape.Cone;
            BtnLine.IsChecked = shape == AreaEffectShape.Line;

            _currentShape = shape;
            BtnCancel.Visibility = Visibility.Visible;
            ShapeSelected?.Invoke(shape);
        }

        private void SelectSize(int sizeInFeet)
        {
            _currentSize = sizeInFeet;

            // Find matching combo item
            for (int i = 0; i < CmbSize.Items.Count; i++)
            {
                if (CmbSize.Items[i] is ComboBoxItem item &&
                    item.Content.ToString() == $"{sizeInFeet} ft")
                {
                    CmbSize.SelectedIndex = i;
                    break;
                }
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            ClearAllRequested?.Invoke();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelDrawing();
        }

        public void CancelDrawing()
        {
            _currentShape = null;
            BtnSphere.IsChecked = false;
            BtnCube.IsChecked = false;
            BtnCone.IsChecked = false;
            BtnLine.IsChecked = false;
            BtnCancel.Visibility = Visibility.Collapsed;
            CancelRequested?.Invoke();
        }
    }
}
