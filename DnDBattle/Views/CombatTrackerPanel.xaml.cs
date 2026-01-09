using DnDBattle.Models;
using DnDBattle.Utils;
using DnDBattle.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for CombatTrackerPanel.xaml
    /// </summary>
    public partial class CombatTrackerPanel : UserControl
    {
        public CombatTrackerPanel()
        {
            InitializeComponent();
        }

        private void InitiativeItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is Token token)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectedToken = token;
                }
            }
        }

    }
}
