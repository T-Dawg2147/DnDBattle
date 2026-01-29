using System.Windows;
using System.Windows.Controls;

namespace DnDBattle.Views
{
    public partial class WallToolbar : UserControl
    {
        public event RoutedEventHandler DrawModeToggled;
        public event RoutedEventHandler SaveRequested;

        public WallToolbar()
        {
            InitializeComponent();
            BtnDrawWalls.Checked += (s, e) => DrawModeToggled?.Invoke(s, e);
            BtnDrawWalls.Unchecked += (s, e) => DrawModeToggled?.Invoke(s, e);
            BtnSaveWalls.Click += (s, e) => SaveRequested?.Invoke(s, e);
        }

        public bool IsDrawMode => BtnDrawWalls.IsChecked == true;
    }
}