using DnDBattle.Models;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.TileService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DnDBattle.Controls
{
    public partial class TileMapEditorControl : UserControl
    {
        // Dependency property for TileMap
        public static readonly DependencyProperty TileMapProperty =
            DependencyProperty.Register(nameof(TileMap), typeof(TileMap), typeof(TileMapEditorControl),
                new PropertyMetadata(null, OnTileMapChanged));

        public TileMap TileMap
        {
            get => (TileMap)GetValue(TileMapProperty);
            set => SetValue(TileMapProperty, value);
        }

        // Dependency property for SelectedTileDefinition
        public static readonly DependencyProperty SelectedTileDefinitionProperty =
            DependencyProperty.Register(nameof(SelectedTileDefinition), typeof(TileDefinition), typeof(TileMapEditorControl),
                new PropertyMetadata(null));

        public TileDefinition SelectedTileDefinition
        {
            get => (TileDefinition)GetValue(SelectedTileDefinitionProperty);
            set => SetValue(SelectedTileDefinitionProperty, value);
        }

        // Editor state
        private enum EditMode { Paint, Erase }
        private EditMode _currentMode = EditMode.Paint;

        private bool _isPanning = false;
        private Point _lastPanPoint;
        private bool _isPainting = false;

        public TileMapEditorControl()
        {
            InitializeComponent();
            DataContext = this;

            Loaded += TileMapEditorControl_Loaded;
        }

        private void TileMapEditorControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (TileMap != null)
            {
                RenderMap();
            }
        }

        private static void OnTileMapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TileMapEditorControl)d;
            control.RenderMap();
        }

        /// <summary>
        /// Render the entire tile map
        /// </summary>
        private void RenderMap()
        {
            if (TileMap == null) return;

            TilesLayer.Children.Clear();
            GridLayer.Children.Clear();

            // Set canvas size
            MapCanvas.Width = TileMap.Width * TileMap.CellSize;
            MapCanvas.Height = TileMap.Height * TileMap.CellSize;

            // Draw grid
            if (TileMap.ShowGrid)
            {
                DrawGrid();
            }

            // Draw all tiles
            foreach (var tile in TileMap.PlacedTiles.OrderBy(t => t.ZIndex ?? 0))
            {
                DrawTile(tile);
            }
        }

        /// <summary>
        /// Draw grid lines
        /// </summary>
        private void DrawGrid()
        {
            if (TileMap == null) return;

            var gridBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            double cellSize = TileMap.CellSize;

            // Vertical lines
            for (int x = 0; x <= TileMap.Width; x++)
            {
                var line = new Line
                {
                    X1 = x * cellSize,
                    Y1 = 0,
                    X2 = x * cellSize,
                    Y2 = TileMap.Height * cellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                GridLayer.Children.Add(line);
            }

            // Horizontal lines
            for (int y = 0; y <= TileMap.Height; y++)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y * cellSize,
                    X2 = TileMap.Width * cellSize,
                    Y2 = y * cellSize,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                GridLayer.Children.Add(line);
            }
        }

        /// <summary>
        /// Draw a single tile instance
        /// </summary>
        private void DrawTile(Tile tile)
        {
            var tileDef = TileLibraryService.Instance.GetTileById(tile.TileDefinitionId);
            if (tileDef == null) return;

            var image = TileImageCacheService.Instance.GetOrLoadImage(tileDef.ImagePath);
            if (image == null) return;

            var tileImage = new Image
            {
                Source = image,
                Width = TileMap.CellSize,
                Height = TileMap.CellSize,
                Stretch = Stretch.Fill,
                Tag = tile // Store reference for identification
            };

            // Apply transformations
            var transformGroup = new TransformGroup();
            if (tile.Rotation != 0)
            {
                transformGroup.Children.Add(new RotateTransform(tile.Rotation, TileMap.CellSize / 2, TileMap.CellSize / 2));
            }
            if (tile.FlipHorizontal)
            {
                transformGroup.Children.Add(new ScaleTransform(-1, 1, TileMap.CellSize / 2, TileMap.CellSize / 2));
            }
            if (tile.FlipVertical)
            {
                transformGroup.Children.Add(new ScaleTransform(1, -1, TileMap.CellSize / 2, TileMap.CellSize / 2));
            }
            tileImage.RenderTransform = transformGroup;

            Canvas.SetLeft(tileImage, tile.GridX * TileMap.CellSize);
            Canvas.SetTop(tileImage, tile.GridY * TileMap.CellSize);
            Canvas.SetZIndex(tileImage, tile.ZIndex ?? tileDef.ZIndex);

            TilesLayer.Children.Add(tileImage);
        }

        #region Mouse Event Handlers

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Pan mode with Ctrl held
                _isPanning = true;
                _lastPanPoint = e.GetPosition(MapBorder);
                MapCanvas.CaptureMouse();
            }
            else
            {
                // Paint/Erase mode
                _isPainting = true;
                ProcessTileAction(e.GetPosition(MapCanvas));
                MapCanvas.CaptureMouse();
            }
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var currentPoint = e.GetPosition(MapBorder);
                var delta = currentPoint - _lastPanPoint;
                PanTransform.X += delta.X;
                PanTransform.Y += delta.Y;
                _lastPanPoint = currentPoint;
            }
            else if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
            {
                ProcessTileAction(e.GetPosition(MapCanvas));
            }
        }

        private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            _isPainting = false;
            MapCanvas.ReleaseMouseCapture();
        }

        private void MapCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Quick erase on right-click
            var pos = e.GetPosition(MapCanvas);
            var gridPos = ScreenToGrid(pos);
            RemoveTileAt(gridPos.X, gridPos.Y);
        }

        private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom with mouse wheel
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            double newScale = ZoomTransform.ScaleX * zoomFactor;
            newScale = Math.Clamp(newScale, 0.25, 4.0);

            ZoomTransform.ScaleX = newScale;
            ZoomTransform.ScaleY = newScale;
        }

        #endregion

        #region Tile Placement Logic

        /// <summary>
        /// Process paint or erase action at mouse position
        /// </summary>
        private void ProcessTileAction(Point screenPos)
        {
            if (TileMap == null) return;

            var gridPos = ScreenToGrid(screenPos);

            if (gridPos.X < 0 || gridPos.X >= TileMap.Width || gridPos.Y < 0 || gridPos.Y >= TileMap.Height)
                return;

            if (_currentMode == EditMode.Paint)
            {
                PlaceTileAt(gridPos.X, gridPos.Y);
            }
            else if (_currentMode == EditMode.Erase)
            {
                RemoveTileAt(gridPos.X, gridPos.Y);
            }
        }

        /// <summary>
        /// Place a tile at grid coordinates
        /// </summary>
        private void PlaceTileAt(int gridX, int gridY)
        {
            if (SelectedTileDefinition == null) return;

            // Remove existing tiles at this position (replace mode)
            RemoveTileAt(gridX, gridY);

            var newTile = new Tile
            {
                TileDefinitionId = SelectedTileDefinition.Id,
                GridX = gridX,
                GridY = gridY
            };

            TileMap.AddTile(newTile);
            DrawTile(newTile);
        }

        /// <summary>
        /// Remove tile(s) at grid coordinates
        /// </summary>
        private void RemoveTileAt(int gridX, int gridY)
        {
            var tilesToRemove = TileMap.GetTilesAt(gridX, gridY).ToList();

            foreach (var tile in tilesToRemove)
            {
                TileMap.RemoveTile(tile);

                // Remove visual
                var visual = TilesLayer.Children.OfType<Image>().FirstOrDefault(img => img.Tag == tile);
                if (visual != null)
                {
                    TilesLayer.Children.Remove(visual);
                }
            }
        }

        /// <summary>
        /// Convert screen position to grid coordinates
        /// </summary>
        private (int X, int Y) ScreenToGrid(Point screenPos)
        {
            if (TileMap == null) return (0, 0);

            int gridX = (int)(screenPos.X / TileMap.CellSize);
            int gridY = (int)(screenPos.Y / TileMap.CellSize);

            return (gridX, gridY);
        }

        #endregion

        #region Toolbar Handlers

        private void PaintMode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = EditMode.Paint;
            BtnPaintMode.FontWeight = FontWeights.Bold;
            BtnEraseMode.FontWeight = FontWeights.Normal;
        }

        private void EraseMode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = EditMode.Erase;
            BtnEraseMode.FontWeight = FontWeights.Bold;
            BtnPaintMode.FontWeight = FontWeights.Normal;
        }

        #endregion

        public Brush BackgroundBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(TileMap?.BackgroundColor ?? "#FF1A1A1A"));
        public string StatusText => $"Mode: {_currentMode} | Tiles: {TileMap?.PlacedTiles.Count ?? 0}";
    }
}