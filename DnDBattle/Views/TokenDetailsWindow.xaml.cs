using DnDBattle.Controls;
using DnDBattle.Models;
using System.Windows;
using System.Windows.Input;

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for TokenDetailsWindow.xaml
    /// </summary>
    public partial class TokenDetailsWindow : Window
    {
        private BattleGridControl BattleGrid = new();

        public TokenDetailsWindow(Token token)
        {
            InitializeComponent();
            DataContext = token;
        }

        private void TxtHP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BattleGrid?.Focus();
                e.Handled = true;
            }
        }
    }
}
