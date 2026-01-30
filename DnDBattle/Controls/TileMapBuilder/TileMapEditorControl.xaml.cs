using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.Mapping_Services;
using DnDBattle.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DnDBattle.Controls.TileMapBuilder
{
    public partial class TileMapEditorControl : UserControl
    {
        private TileMapBuilderViewModel _viewModel;
        private readonly TileImageCacheService _imageCache;

        // Pan/Zoom state
        private double _zoom = 1.0;
        private Point _panOffset = new Point(0, 0);
        private Point _lastMousePosition;
        private bool _isPanning = false;
        private bool _isPainting = false;

        // Grid settings
        private double _cellSize = 48.0;
        private bool _showGrid = true;

        // Cached visuals for performance
        private readonly Dictionary<string, Image> _tileVisuals = new Dictionary<string, Image>();

        public TileMapEditorControl()
        {
            InitializeComponent();
            _imageCache = TileImageCacheService.Instance;

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        #region Properties

        public bool ShowGrid
        {
            get => _showGrid;
            set
            {
                _showGrid = value;
                DrawGridLines();
            }
        }

        public double CellSize
        {
            get => _cellSize;
            set
            {
                _cellSize = value;
                RedrawAll();
            }
        }

        #endregion

        #region Initialization

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TileMapBuilderViewModel vm)
            {
                _viewModel = vm;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

                if (_viewModel.CurrentMap != null)
                {
                    _cellSize = _viewModel.CurrentMap.GridCellSize;
                }

                RedrawAll();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Focus();
            CenterView();
            RedrawAll();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGridLines();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TileMapBuilderViewModel.CurrentMap):
                    if (_viewModel.CurrentMap != null)
                        _cellSize = _viewModel.CurrentMap.GridCellSize;
                    CenterView();
                    RedrawAll();
                    break;

                case nameof(TileMapBuilderViewModel.MapTiles):
                    RedrawTiles();
                    break;

                case nameof(TileMapBuilderViewModel.SelectedTileDefinition):
                case nameof(TileMapBuilderViewModel.BrushRotation):
                    UpdatePreview();
                    break;

                case nameof(TileMapBuilderViewModel.CurrentTool):
                    UpdateCursor();
                    break;

                case nameof(TileMapBuilderViewModel.IsLoading):
                    LoadingOverlay.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                    break;
            }
        }

        #endregion

        #region Mouse Handling

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            _lastMousePosition = e.GetPosition(CanvasBorder);

            if (_viewModel == null) return;

            var worldPos = ScreenToWorld(e.GetPosition(MapCanvas));
            var gridPos = WorldToGrid(worldPos);

            switch (_viewModel.CurrentTool)
            {
                case EditorTool.Paint:
                    _isPainting = true;
                    _viewModel.PaintTile(gridPos.X, gridPos.Y);
                    break;

                case EditorTool.Erase:
                    _isPainting = true;
                    _viewModel.EraseTile(gridPos.X, gridPos.Y);
                    break;

                case EditorTool.Pick:
                    _viewModel.PickTile(gridPos.X, gridPos.Y);
                    break;
            }

            CaptureMouse();
            e.Handled = true;
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPainting = false;
            ReleaseMouseCapture();
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isPanning = true;
            _lastMousePosition = e.GetPosition(CanvasBorder);
            Cursor = Cursors.Hand;
            CaptureMouse();
            e.Handled = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPos = e.GetPosition(CanvasBorder);
            var worldPos = ScreenToWorld(e.GetPosition(MapCanvas));
            var gridPos = WorldToGrid(worldPos);

            // Update coordinates display
            TxtCoordinates.Text = $"X: {gridPos.X}, Y: {gridPos.Y}";

            // Handle panning
            if (_isPanning && e.RightButton == MouseButtonState.Pressed)
            {
                var delta = currentPos - _lastMousePosition;
                _panOffset.X += delta.X;
                _panOffset.Y += delta.Y;
                ApplyTransform();
                _lastMousePosition = currentPos;
                return;
            }

            // Handle painting/erasing while dragging
            if (_isPainting && e.LeftButton == MouseButtonState.Pressed && _viewModel != null)
            {
                if (_viewModel.CurrentTool == EditorTool.Paint)
                    _viewModel.PaintTile(gridPos.X, gridPos.Y);
                else if (_viewModel.CurrentTool == EditorTool.Erase)
                    _viewModel.EraseTile(gridPos.X, gridPos.Y);
            }

            // Update preview position
            UpdatePreviewPosition(gridPos);
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var mousePos = e.GetPosition(MapCanvas);

            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            double newZoom = Math.Clamp(_zoom * zoomFactor, 0.25, 4.0);

            // Zoom toward mouse position
            var worldPosBefore = ScreenToWorld(mousePos);

            _zoom = newZoom;
            ApplyTransform();

            var worldPosAfter = ScreenToWorld(mousePos);
            _panOffset.X += (worldPosAfter.X - worldPosBefore.X) * _zoom;
            _panOffset.Y += (worldPosAfter.Y - worldPosBefore.Y) * _zoom;
            ApplyTransform();

            TxtZoom.Text = $"{(int)(_zoom * 100)}%";
            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                _isPanning = false;
                UpdateCursor();
                ReleaseMouseCapture();
            }
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_viewModel == null) return;

            switch (e.Key)
            {
                case Key.P:
                    _viewModel.SetToolCommand.Execute(EditorTool.Paint);
                    break;
                case Key.E:
                    _viewModel.SetToolCommand.Execute(EditorTool.Erase);
                    break;
                case Key.I:
                    _viewModel.SetToolCommand.Execute(EditorTool.Pick);
                    break;
                case Key.R:
                    _viewModel.RotateBrushCommand.Execute(null);
                    break;
                case Key.OemPlus:
                case Key.Add:
                    ZoomIn();
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    ZoomOut();
                    break;
                case Key.D0:
                case Key.NumPad0:
                    ResetView();
                    break;
            }

            base.OnKeyDown(e);
        }

        #endregion

        #region Zoom Controls

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomIn();
        private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomOut();

        private void ZoomIn()
        {
            _zoom = Math.Min(_zoom * 1.2, 4.0);
            ApplyTransform();
            TxtZoom.Text = $"{(int)(_zoom * 100)}%";
        }

        private void ZoomOut()
        {
            _zoom = Math.Max(_zoom / 1.2, 0.25);
            ApplyTransform();
            TxtZoom.Text = $"{(int)(_zoom * 100)}%";
        }

        private void ResetView()
        {
            _zoom = 1.0;
            CenterView();
            TxtZoom.Text = "100%";
        }

        private void CenterView()
        {
            if (_viewModel?.CurrentMap == null) return;

            double mapWidth = _viewModel.CurrentMap.WidthInSquares * _cellSize;
            double mapHeight = _viewModel.CurrentMap.HeightInSquares * _cellSize;

            _panOffset.X = (ActualWidth - mapWidth * _zoom) / 2;
            _panOffset.Y = (ActualHeight - mapHeight * _zoom) / 2;

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            ZoomTransform.ScaleX = _zoom;
            ZoomTransform.ScaleY = _zoom;
            PanTransform.X = _panOffset.X;
            PanTransform.Y = _panOffset.Y;

            DrawGridLines();
        }

        #endregion

        #region Coordinate Conversion

        private Point ScreenToWorld(Point screenPoint)
        {
            return new Point(
                (screenPoint.X - _panOffset.X) / _zoom,
                (screenPoint.Y - _panOffset.Y) / _zoom);
        }

        private Point WorldToScreen(Point worldPoint)
        {
            return new Point(
                worldPoint.X * _zoom + _panOffset.X,
                worldPoint.Y * _zoom + _panOffset.Y);
        }

        private (int X, int Y) WorldToGrid(Point worldPoint)
        {
            return (
                (int)Math.Floor(worldPoint.X / _cellSize),
                (int)Math.Floor(worldPoint.Y / _cellSize));
        }

        private Point GridToWorld(int gridX, int gridY)
        {
            return new Point(gridX * _cellSize, gridY * _cellSize);
        }

        #endregion

        #region Drawing

        private void RedrawAll()
        {
            DrawGridLines();
            RedrawTiles();
            UpdatePreview();
        }

        private void DrawGridLines()
        {
            GridLinesCanvas.Children.Clear();

            if (!_showGrid || _viewModel?.CurrentMap == null) return;

            int cols = _viewModel.CurrentMap.WidthInSquares;
            int rows = _viewModel.CurrentMap.HeightInSquares;

            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1);
            gridPen.Freeze();

            var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 100, 100, 100)), 2);
            borderPen.Freeze();

            // Draw grid lines
            for (int x = 0; x <= cols; x++)
            {
                var line = new Line
                {
                    X1 = x * _cellSize,
                    Y1 = 0,
                    X2 = x * _cellSize,
                    Y2 = rows * _cellSize,
                    Stroke = gridPen.Brush,
                    StrokeThickness = gridPen.Thickness
                };
                GridLinesCanvas.Children.Add(line);
            }

            for (int y = 0; y <= rows; y++)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y * _cellSize,
                    X2 = cols * _cellSize,
                    Y2 = y * _cellSize,
                    Stroke = gridPen.Brush,
                    StrokeThickness = gridPen.Thickness
                };
                GridLinesCanvas.Children.Add(line);
            }

            // Draw border
            var border = new Rectangle
            {
                Width = cols * _cellSize,
                Height = rows * _cellSize,
                Stroke = borderPen.Brush,
                StrokeThickness = borderPen.Thickness,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(border, 0);
            Canvas.SetTop(border, 0);
            GridLinesCanvas.Children.Add(border);
        }

        private void RedrawTiles()
        {
            TilesCanvas.Children.Clear();
            _tileVisuals.Clear();

            if (_viewModel?.CurrentMap == null) return;

            foreach (var tile in _viewModel.MapTiles)
            {
                AddTileVisual(tile);
            }
        }

        private void AddTileVisual(Tile tile)
        {
            var definition = _viewModel.GetTileDefinition(tile.TileDefinitionId);
            if (definition?.CachedImage == null) return;

            var image = new Image
            {
                Source = definition.CachedImage,
                Width = _cellSize * definition.WidthInSquares,
                Height = _cellSize * definition.HeightInSquares,
                Stretch = Stretch.Fill,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            // Apply rotation
            if (tile.Rotation != 0)
            {
                image.RenderTransform = new RotateTransform(tile.Rotation);
            }

            // Use high-quality scaling for zoomed view, nearest neighbor for pixel art
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            Canvas.SetLeft(image, tile.GridX * _cellSize);
            Canvas.SetTop(image, tile.GridY * _cellSize);
            Canvas.SetZIndex(image, tile.Layer);

            TilesCanvas.Children.Add(image);
            _tileVisuals[tile.Id.ToString()] = image;
        }

        #endregion

        #region Preview

        private void UpdatePreview()
        {
            if (_viewModel?.SelectedTileDefinition?.CachedImage == null ||
                _viewModel.CurrentTool != EditorTool.Paint)
            {
                PreviewTile.Visibility = Visibility.Collapsed;
                return;
            }

            PreviewImage.Source = _viewModel.SelectedTileDefinition.CachedImage;
            PreviewTile.Width = _cellSize * _viewModel.SelectedTileDefinition.WidthInSquares;
            PreviewTile.Height = _cellSize * _viewModel.SelectedTileDefinition.HeightInSquares;

            if (_viewModel.BrushRotation != 0)
            {
                PreviewImage.RenderTransformOrigin = new Point(0.5, 0.5);
                PreviewImage.RenderTransform = new RotateTransform(_viewModel.BrushRotation);
            }
            else
            {
                PreviewImage.RenderTransform = null;
            }

            PreviewTile.Visibility = Visibility.Visible;
        }

        private void UpdatePreviewPosition((int X, int Y) gridPos)
        {
            if (PreviewTile.Visibility != Visibility.Visible) return;

            Canvas.SetLeft(PreviewTile, gridPos.X * _cellSize);
            Canvas.SetTop(PreviewTile, gridPos.Y * _cellSize);
        }

        #endregion

        #region Cursor

        private void UpdateCursor()
        {
            if (_viewModel == null) return;

            Cursor = _viewModel.CurrentTool switch
            {
                EditorTool.Paint => Cursors.Pen,
                EditorTool.Erase => Cursors.Cross,
                EditorTool.Pick => Cursors.Hand,
                _ => Cursors.Arrow
            };
        }

        #endregion
    }
}