using System.Windows;
using DnDBattle.ViewModels;

namespace DnDBattle.Views
{
    public partial class JoinGameWindow : Window
    {
        public JoinGameWindow()
        {
            InitializeComponent();
            DataContext = new PlayerClientViewModel();
        }
    }
}
