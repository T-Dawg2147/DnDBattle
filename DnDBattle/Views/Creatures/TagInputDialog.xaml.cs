using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Shapes;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Encounters;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Views.Creatures;

namespace DnDBattle.Views.Creatures
{
    /// <summary>
    /// Interaction logic for TagInputDialog.xaml
    /// </summary>
    public partial class TagInputDialog : Window
    {
        private static readonly string[] CommonTags = new[]
        {
            "favourite", "boss", "monster", "npc", "elite", "legendary",
            "undead", "fiend", "beast", "humanoid", "dragon", "spellcaster",
            "melee", "ranged", "support", "tank"
        };

        public ObservableCollection<TagItem> ExistingTags { get; } = new ObservableCollection<TagItem>();

        public List<string> SelectedTags { get; private set; } = new List<string>();

        private readonly HashSet<string> _initialTags;

        public TagInputDialog(IEnumerable<string> allExistingTags, IEnumerable<string> currentCreatureTags = null)
        {
            InitializeComponent();
            _initialTags = new HashSet<string>(
                currentCreatureTags ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            var uniqueTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in allExistingTags ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(tag))
                    uniqueTags.Add(tag);
            }

            foreach (var tag in uniqueTags.OrderBy(t => t))
            {
                ExistingTags.Add(new TagItem
                {
                    Name = tag,
                    IsSelected = _initialTags.Contains(tag)
                });
            }

            TagButtonsPanel.ItemsSource = ExistingTags;

            PopulateCommonTags();

            Loaded += (s, e) => TxtNewTag.Focus();
        }

        private void PopulateCommonTags()
        {
            foreach (var tag in CommonTags)
            {
                var btn = new ToggleButton()
                {
                    Content = tag,
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 6, 6),
                    MinWidth = 60,
                    IsChecked = _initialTags.Contains(tag),
                    Tag = tag
                };

                btn.Style = (Style)FindResource("TagToggleButtonStyle");
                btn.Checked += CommonTag_Toggled;
                btn.Unchecked += CommonTag_Toggled;

                CommonTagsPanel.Children.Add(btn);
            }
        }

        private void CommonTag_Toggled(object sender, RoutedEventArgs e)
        {

        }

        private void TxtNewTag_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNewTag();
                e.Handled = true;
            }
        }

        private void BtnAddTag_Click(object sender, RoutedEventArgs e)
        {
            AddNewTag();
        }

        private void AddNewTag()
        {
            var newTag = TxtNewTag.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newTag))
                return;

            var existing = ExistingTags.FirstOrDefault(t =>
                string.Equals(t.Name, newTag, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Select it
                existing.IsSelected = true;
            }
            else
            {
                ExistingTags.Add(new TagItem { Name = newTag, IsSelected = true });
            }

            TxtNewTag.Clear();
            TxtNewTag.Focus();
        }

        private List<string> GetSelectedTags()
        {
            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in ExistingTags.Where(t => t.IsSelected))
            {
                selected.Add(tag.Name);
            }

            foreach (ToggleButton btn in CommonTagsPanel.Children)
            {
                if (btn.IsChecked == true && btn.Tag is string tagName)
                {
                    selected.Add(tagName);
                }
            }

            return selected.OrderBy(t => t).ToList();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedTags = GetSelectedTags();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    /// <summary>
    /// Represents a tag item with selection state
    /// </summary>
    public class TagItem : INotifyPropertyChanged
    {
        private string _name;
        private bool _isSelected;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
