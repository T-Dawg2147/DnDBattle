using DnDBattle.Models.Tiles;
using DnDBattle.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DnDBattle.Controls.TileMapBuilder
{
    public partial class TilePaletteControl : UserControl
    {
        private TileMapBuilderViewModel _viewModel;
        private string _searchText = "";

        public TilePaletteControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TileMapBuilderViewModel vm)
            {
                _viewModel = vm;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                UpdateTileCount();
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TileMapBuilderViewModel.FilteredTiles))
            {
                UpdateTileCount();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.RefreshPaletteCommand.Execute(null);
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.OpenTilesFolderCommand.Execute(null);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = TxtSearch.Text?.Trim().ToLowerInvariant() ?? "";
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (_viewModel == null) return;

            var view = CollectionViewSource.GetDefaultView(_viewModel.FilteredTiles);
            if (view != null)
            {
                view.Filter = item =>
                {
                    if (string.IsNullOrEmpty(_searchText)) return true;
                    if (item is TileDefinition tile)
                    {
                        return (tile.Name?.ToLowerInvariant().Contains(_searchText) ?? false) ||
                               (tile.Category?.ToLowerInvariant().Contains(_searchText) ?? false);
                    }
                    return true;
                };
            }
            UpdateTileCount();
        }

        private void UpdateTileCount()
        {
            if (_viewModel == null) return;

            var view = CollectionViewSource.GetDefaultView(_viewModel.FilteredTiles);
            int visibleCount = view?.Cast<object>().Count() ?? 0;
            int totalCount = _viewModel.AvailableTiles.Count;

            TxtTileCount.Text = visibleCount == totalCount
                ? $"{totalCount} tiles"
                : $"{visibleCount} of {totalCount} tiles";
        }
    }

    #region Value Converters


    // These should be moved into there own file, into Converters
    public class GreaterThanOneToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 1 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToVisibilityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int intValue)
                return intValue == 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}