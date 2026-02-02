using DnDBattle.Models.Tiles;
using DnDBattle.Services.TileMap;
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
        private enum EditMode { Paint, Erase, Properties }
        private EditMode _currentMode = EditMode.Paint;

        private bool _isPanning = false;
        private Point _lastPanPoint;
        private bool _isPainting = false;
        private bool _showDMView = true; // DM view on by default
        public ScaleTransform ZoomTransformation => ZoomTransform;
        public TranslateTransform PanTransformation => PanTransform;

        // Events
        public event Action<Tile> TileRightClicked;

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
            MetadataLayer.Children.Clear();

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

            // Draw metadata overlays (DM view)
            if (_showDMView)
            {
                DrawMetadataOverlays();
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

        /// <summary>
        /// Draw DM-only metadata overlays on tiles
        /// </summary>
        private void DrawMetadataOverlays()
        {
            if (TileMap == null) return;

            foreach (var tile in TileMap.PlacedTiles.Where(t => t.HasMetadata))
            {
                DrawMetadataIndicator(tile);
            }
        }

        /// <summary>
        /// Draw a visual indicator for a tile with metadata
        /// </summary>
        private void DrawMetadataIndicator(Tile tile)
        {
            double cellSize = TileMap.CellSize;
            double x = tile.GridX * cellSize;
            double y = tile.GridY * cellSize;

            // Create overlay border
            var border = new Border
            {
                Width = cellSize,
                Height = cellSize,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                IsHitTestVisible = false // Don't interfere with mouse events
            };

            // Determine color based on metadata type
            var metadataTypes = tile.Metadata.Select(m => m.Type).ToList();
            border.BorderBrush = GetMetadataBorderBrush(metadataTypes);

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            Canvas.SetZIndex(border, 1000); // Always on top

            MetadataLayer.Children.Add(border);

            // Add icon indicators
            var iconPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                IsHitTestVisible = false
            };

            // Add up to 3 icons
            int iconCount = 0;
            foreach (var metadata in tile.Metadata.Take(3))
            {
                var icon = new TextBlock
                {
                    Text = metadata.Type.GetIcon(),
                    FontSize = cellSize * 0.25,
                    Margin = new Thickness(2),
                    Foreground = Brushes.White
                };
                iconPanel.Children.Add(icon);
                iconCount++;
            }

            // Show "+X" if more than 3
            if (tile.Metadata.Count > 3)
            {
                var moreText = new TextBlock
                {
                    Text = $"+{tile.Metadata.Count - 3}",
                    FontSize = cellSize * 0.2,
                    Margin = new Thickness(2),
                    Foreground = Brushes.Yellow,
                    FontWeight = FontWeights.Bold
                };
                iconPanel.Children.Add(moreText);
            }

            Canvas.SetLeft(iconPanel, x);
            Canvas.SetTop(iconPanel, y);
            Canvas.SetZIndex(iconPanel, 1001);

            MetadataLayer.Children.Add(iconPanel);

            // Add tooltip with metadata details
            var tooltip = CreateMetadataTooltip(tile);
            border.ToolTip = tooltip;
            iconPanel.ToolTip = tooltip;
        }

        /// <summary>
        /// Get border brush based on metadata types
        /// </summary>
        private Brush GetMetadataBorderBrush(List<TileMetadataType> types)
        {
            // Priority: Trap > Hazard > Secret > Interactive > Others
            if (types.Contains(TileMetadataType.Trap))
                return new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)); // Red

            if (types.Contains(TileMetadataType.Hazard))
                return new SolidColorBrush(Color.FromArgb(200, 255, 152, 0)); // Orange

            if (types.Contains(TileMetadataType.Secret))
                return new SolidColorBrush(Color.FromArgb(200, 255, 235, 59)); // Yellow

            if (types.Contains(TileMetadataType.Interactive))
                return new SolidColorBrush(Color.FromArgb(200, 33, 150, 243)); // Blue

            if (types.Contains(TileMetadataType.Trigger))
                return new SolidColorBrush(Color.FromArgb(200, 156, 39, 176)); // Purple

            if (types.Contains(TileMetadataType.Spawn))
                return new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)); // Red

            if (types.Contains(TileMetadataType.Teleporter))
                return new SolidColorBrush(Color.FromArgb(200, 0, 188, 212)); // Cyan

            if (types.Contains(TileMetadataType.Healing))
                return new SolidColorBrush(Color.FromArgb(200, 76, 175, 80)); // Green

            return new SolidColorBrush(Color.FromArgb(200, 158, 158, 158)); // Gray
        }

        /// <summary>
        /// Create tooltip showing metadata details
        /// </summary>
        private ToolTip CreateMetadataTooltip(Tile tile)
        {
            var tooltip = new ToolTip
            {
                Background = (Brush)Application.Current.Resources["Brush_Background_Control"],
                BorderBrush = (Brush)Application.Current.Resources["Brush_Border_Normal"],
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10)
            };

            var panel = new StackPanel();

            // Header
            var header = new TextBlock
            {
                Text = $"📍 Tile ({tile.GridX}, {tile.GridY})",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = (Brush)Application.Current.Resources["Brush_Text_Primary"],
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(header);

            // Metadata list
            foreach (var metadata in tile.Metadata)
            {
                var metaPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 3, 0, 3)
                };

                var icon = new TextBlock
                {
                    Text = metadata.Type.GetIcon(),
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                metaPanel.Children.Add(icon);

                var info = new StackPanel();

                var nameText = new TextBlock
                {
                    Text = metadata.Name ?? metadata.Type.GetDisplayName(),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["Brush_Text_Primary"]
                };
                info.Children.Add(nameText);

                var typeText = new TextBlock
                {
                    Text = metadata.Type.GetDisplayName(),
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["Brush_Text_Hint"]
                };
                info.Children.Add(typeText);

                // Add trap-specific info
                if (metadata is TrapMetadata trap)
                {
                    var trapInfo = new TextBlock
                    {
                        Text = $"DC {trap.DetectionDC} Perception | {trap.DamageDice} {trap.DamageType.GetDisplayName()}",
                        FontSize = 10,
                        Foreground = (Brush)Application.Current.Resources["Brush_Warning"]
                    };
                    info.Children.Add(trapInfo);

                    if (trap.IsDetected)
                    {
                        var detected = new TextBlock
                        {
                            Text = "🔍 Detected",
                            FontSize = 10,
                            Foreground = (Brush)Application.Current.Resources["Brush_Success"]
                        };
                        info.Children.Add(detected);
                    }

                    if (trap.IsDisarmed)
                    {
                        var disarmed = new TextBlock
                        {
                            Text = "✅ Disarmed",
                            FontSize = 10,
                            Foreground = (Brush)Application.Current.Resources["Brush_Success"]
                        };
                        info.Children.Add(disarmed);
                    }
                }

                metaPanel.Children.Add(info);
                panel.Children.Add(metaPanel);
            }

            // Footer hint
            var footer = new TextBlock
            {
                Text = "Right-click to edit properties",
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                Foreground = (Brush)Application.Current.Resources["Brush_Text_Hint"],
                Margin = new Thickness(0, 8, 0, 0)
            };
            panel.Children.Add(footer);

            tooltip.Content = panel;
            return tooltip;
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
            else if (_currentMode == EditMode.Properties)
            {
                // Properties mode - select tile
                var pos = e.GetPosition(MapCanvas);
                var gridPos = ScreenToGrid(pos);
                var tile = GetTileAt(gridPos.X, gridPos.Y);

                if (tile != null)
                {
                    TileRightClicked?.Invoke(tile);
                }
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
            // Right-click - open properties for tile
            var pos = e.GetPosition(MapCanvas);
            var gridPos = ScreenToGrid(pos);
            var tile = GetTileAt(gridPos.X, gridPos.Y);

            if (tile != null)
            {
                TileRightClicked?.Invoke(tile);
            }
            else
            {
                // Quick erase on empty space
                RemoveTileAt(gridPos.X, gridPos.Y);
            }
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

            // Redraw metadata overlays
            if (_showDMView)
            {
                MetadataLayer.Children.Clear();
                DrawMetadataOverlays();
            }
        }

        /// <summary>
        /// Get tile at grid position (returns topmost if multiple)
        /// </summary>
        private Tile GetTileAt(int gridX, int gridY)
        {
            return TileMap?.GetTilesAt(gridX, gridY).OrderByDescending(t => t.ZIndex ?? 0).FirstOrDefault();
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
            BtnPropertiesMode.FontWeight = FontWeights.Normal;
        }

        private void EraseMode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = EditMode.Erase;
            BtnEraseMode.FontWeight = FontWeights.Bold;
            BtnPaintMode.FontWeight = FontWeights.Normal;
            BtnPropertiesMode.FontWeight = FontWeights.Normal;
        }

        private void PropertiesMode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = EditMode.Properties;
            BtnPropertiesMode.FontWeight = FontWeights.Bold;
            BtnPaintMode.FontWeight = FontWeights.Normal;
            BtnEraseMode.FontWeight = FontWeights.Normal;
        }

        private void ToggleDMView_Click(object sender, RoutedEventArgs e)
        {
            _showDMView = !_showDMView;

            if (_showDMView)
            {
                MetadataLayer.Children.Clear();
                DrawMetadataOverlays();
                BtnDMView.FontWeight = FontWeights.Bold;
            }
            else
            {
                MetadataLayer.Children.Clear();
                BtnDMView.FontWeight = FontWeights.Normal;
            }
        }

        #endregion

        public Brush BackgroundBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(TileMap?.BackgroundColor ?? "#FF1A1A1A"));
        public string StatusText => $"Mode: {_currentMode} | Tiles: {TileMap?.PlacedTiles.Count ?? 0} | DM View: {(_showDMView ? "ON" : "OFF")}";
    }
}