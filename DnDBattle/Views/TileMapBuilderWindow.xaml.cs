using DnDBattle.ViewModels;
using System.Windows;

namespace DnDBattle.Views
{
    public partial class TileMapBuilderWindow : Window
    {
        public TileMapBuilderWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TileMapBuilderViewModel vm)
            {
                await vm.LoadPaletteCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Gets the current tile map for use in the battle grid.
        /// </summary>
        public Models.Tiles.TileMap GetCurrentMap()
        {
            return (DataContext as TileMapBuilderViewModel)?.CurrentMap;
        }
    }
}