using DnDBattle.Models;
using DnDBattle.Models.Actions;
using DnDBattle.Models.Tiles;
using DnDBattle.Services;
using DnDBattle.Services.FogOfWar;
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
        private FogOfWarState _fogOfWar = new FogOfWarState();

        private bool _recordingUndo = true;

        private List<PlacedTile> _clipboard = new List<PlacedTile>();
        private (int X, int Y) _clipboardOrigin;

        private enum EditMode { Paint, Erase, Properties }
        private EditMode _currentMode = EditMode.Paint;

        private int _currentRotation = 0;
        private bool _currentFlipH = false;
        private bool _currentFlipV = false;

        private bool _isPanning = false;
        private Point _lastPanPoint;
        private bool _isPainting = false;
        private bool _showDMView = true; // DM view on by default
        public ScaleTransform ZoomTransformation => ZoomTransform;
        public TranslateTransform PanTransformation => PanTransform;

        // Layers state
        private TileLayer _activeLayer = TileLayer.Floor;
        private HashSet<TileLayer> _visibleLayers = new HashSet<TileLayer>()
        {
            TileLayer.Floor, TileLayer.Terrain, TileLayer.Wall, TileLayer.Door,
            TileLayer.Furniture, TileLayer.Props, TileLayer.Effects, TileLayer.Roof
        };

        // Events
        public event Action<PlacedTile> TileRightClicked;

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

        private void ActiveLayer_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CmbActiveLayer.SelectedItem is ComboBoxItem item && item.Tag is string layerName)
            {
                _activeLayer = Enum.Parse<TileLayer>(layerName);

                // TODO - Implement palette filtering
            }
        }

        private void LayerVisibility_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is string layerName)
            {
                var layer = Enum.Parse<TileLayer>(layerName);

                if (cb.IsChecked == true)
                    _visibleLayers.Add(layer);
                else
                    _visibleLayers.Remove(layer);

                RenderMap();
            }
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

            MapCanvas.Width = TileMap.Width * TileMap.CellSize;
            MapCanvas.Height = TileMap.Height * TileMap.CellSize;

            if (TileMap.ShowGrid)
            {
                DrawGrid();
            }

            foreach (var placedTile in TileMap.PlacedTiles.OrderBy(t => t.ZIndex ?? 0))
            {
                DrawTile(placedTile);
            }

            if (_showDMView)
            {
                DrawMetadataOverlays();
            }

            // NEW: Render fog of war
            if (_fogOfWar.IsEnabled)
            {
                RenderFogOfWar();
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
        private void DrawTile(PlacedTile tile)
        {
            var tileDef = TileLibraryService.Instance.GetTileById(tile.TileDefinitionId.ToString());
            if (tileDef == null) return;

            // Check layer visibility
            if (!_visibleLayers.Contains(tileDef.Layer))
                return;

            var image = TileImageCacheService.Instance.GetOrLoadImage(tileDef.ImagePath);
            if (image == null) return;

            var tileImage = new Image
            {
                Source = image,
                Width = TileMap.CellSize,
                Height = TileMap.CellSize,
                Stretch = Stretch.Fill,
                Tag = tile
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

            // Use layer-aware Z-index
            Canvas.SetZIndex(tileImage, tile.GetEffectiveZIndex(tileDef));

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
        private void DrawMetadataIndicator(PlacedTile tile)
        {
            double cellSize = TileMap.CellSize;
            double x = tile.GridX * cellSize;
            double y = tile.GridY * cellSize;

            // Create pulsing border animation for active metadata
            var border = new Border
            {
                Width = cellSize,
                Height = cellSize,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(4),
                IsHitTestVisible = false
            };

            var metadataTypes = tile.Metadata.Select(m => m.Type).ToList();
            border.BorderBrush = GetMetadataBorderBrush(metadataTypes);

            // Add glow effect for traps
            if (metadataTypes.Contains(TileMetadataType.Trap))
            {
                border.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Red,
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.8
                };
            }

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);
            Canvas.SetZIndex(border, 1000);

            MetadataLayer.Children.Add(border);

            // Enhanced icon panel with better layout
            var iconPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                IsHitTestVisible = false
            };

            foreach (var metadata in tile.Metadata.Take(4))
            {
                var iconBorder = new Border
                {
                    Background = GetMetadataIconBackground(metadata.Type),
                    Padding = new Thickness(4),
                    Margin = new Thickness(2)
                };

                var icon = new TextBlock
                {
                    Text = metadata.Type.GetIcon(),
                    FontSize = cellSize * 0.25,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                iconBorder.Child = icon;
                iconPanel.Children.Add(iconBorder);
            }

            if (tile.Metadata.Count > 4)
            {
                var moreText = new TextBlock
                {
                    Text = $"+{tile.Metadata.Count - 4}",
                    FontSize = cellSize * 0.18,
                    Margin = new Thickness(4),
                    Foreground = Brushes.Yellow,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                iconPanel.Children.Add(moreText);
            }

            Canvas.SetLeft(iconPanel, x);
            Canvas.SetTop(iconPanel, y);
            Canvas.SetZIndex(iconPanel, 1001);

            MetadataLayer.Children.Add(iconPanel);

            // Add spawn preview if exists
            var spawns = tile.Metadata.OfType<SpawnMetadata>().ToList();
            if (spawns.Any())
            {
                DrawSpawnPreview(tile, spawns.First());
            }

            // Enhanced tooltip
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
        private ToolTip CreateMetadataTooltip(PlacedTile tile)
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

        /// <summary>
        /// Draw spawn point radius preview
        /// </summary>
        private void DrawSpawnPreview(PlacedTile tile, SpawnMetadata spawn)
        {
            if (spawn.SpawnRadius == 0) return;

            double cellSize = TileMap.CellSize;
            double centerX = (tile.GridX + 0.5) * cellSize;
            double centerY = (tile.GridY + 0.5) * cellSize;
            double radius = spawn.SpawnRadius * cellSize;

            // Draw spawn radius circle
            var circle = new System.Windows.Shapes.Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = new SolidColorBrush(Color.FromArgb(150, 244, 67, 54)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(30, 244, 67, 54)),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(circle, centerX - radius);
            Canvas.SetTop(circle, centerY - radius);
            Canvas.SetZIndex(circle, 999);

            MetadataLayer.Children.Add(circle);

            // Add spawn count label
            var label = new TextBlock
            {
                Text = $"{spawn.SpawnCount}× {spawn.CreatureName}",
                FontSize = cellSize * 0.2,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(180, 244, 67, 54)),
                Padding = new Thickness(0, 4, 0, 2),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(label, centerX - (label.ActualWidth / 2));
            Canvas.SetTop(label, centerY + cellSize * 0.3);
            Canvas.SetZIndex(label, 1002);

            MetadataLayer.Children.Add(label);
        }

        #region For Of War

        private void RenderFogOfWar()
        {
            if (TileMap == null || !_fogOfWar.IsEnabled) return;

            double cellSize = TileMap.CellSize;

            for (int x = 0; x < TileMap.Width; x++)
            {
                for (int y = 0; y < TileMap.Height; y++)
                {
                    bool isRevealed = _fogOfWar.IsTileRevealed(x, y);
                    bool isVisible = _fogOfWar.IsTileVisible(x, y);

                    System.Windows.Shapes.Rectangle fogTile = null;

                    if (!isRevealed)
                    {
                        // Completely hidden - black fog
                        fogTile = new System.Windows.Shapes.Rectangle
                        {
                            Width = cellSize,
                            Height = cellSize,
                            Fill = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)),
                            IsHitTestVisible = false
                        };
                    }
                    else if (_fogOfWar.Mode == FogMode.Dynamic && !isVisible)
                    {
                        // Revealed but not currently visible - gray fog
                        fogTile = new System.Windows.Shapes.Rectangle
                        {
                            Width = cellSize,
                            Height = cellSize,
                            Fill = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                            IsHitTestVisible = false
                        };
                    }

                    if (fogTile != null)
                    {
                        Canvas.SetLeft(fogTile, x * cellSize);
                        Canvas.SetTop(fogTile, y * cellSize);
                        Canvas.SetZIndex(fogTile, 2000); // Above everything
                        MetadataLayer.Children.Add(fogTile);
                    }
                }
            }
        }

        // Add method to toggle fog of war
        public void ToggleFogOfWar(bool enabled)
        {
            _fogOfWar.IsEnabled = enabled;
            RenderMap();
        }

        // Add method to reveal area (when players move)
        public void RevealFogAroundToken(Token token, int visionRange)
        {
            _fogOfWar.RevealArea(token.GridX, token.GridY, visionRange);
            RenderMap();
        }

        #endregion

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

            var tilesToRemove = TileMap.GetTilesAt(gridX, gridY)
                .Where(t =>
                {
                    var def = TileLibraryService.Instance.GetTileById(t.TileDefinitionId.ToString());
                    return def?.Layer == _activeLayer;
                })
                .ToList();

            var newTile = new PlacedTile
            {
                TileDefinitionId = Guid.Parse(SelectedTileDefinition.Id),
                GridX = gridX,
                GridY = gridY,
                Rotation = _currentRotation,        // Apply rotation
                FlipHorizontal = _currentFlipH,     // Apply flip
                FlipVertical = _currentFlipV        // Apply flip
            };

            // Record undo
            if (_recordingUndo && tilesToRemove.Any())
            {
                var batchAction = new TileBatchAction(TileMap, new List<PlacedTile> { newTile }, tilesToRemove, "Replace Tile");
                UndoManager.Record(batchAction);
            }
            else if (_recordingUndo)
            {
                var action = new TilePlaceAction(TileMap, newTile);
                UndoManager.Record(action);
            }

            // Remove old tiles on this layer
            foreach (var tile in tilesToRemove)
            {
                TileMap.RemoveTile(tile);
                var visual = TilesLayer.Children.OfType<Image>().FirstOrDefault(img => img.Tag == tile);
                if (visual != null)
                {
                    TilesLayer.Children.Remove(visual);
                }
            }

            TileMap.AddTile(newTile);
            DrawTile(newTile);
        }

        // Update RemoveTileAt method to record undo:
        private void RemoveTileAt(int gridX, int gridY)
        {
            var tilesToRemove = TileMap.GetTilesAt(gridX, gridY).ToList();

            if (tilesToRemove.Count == 0) return;

            // Record undo action
            if (_recordingUndo)
            {
                if (tilesToRemove.Count == 1)
                {
                    var action = new TileRemoveAction(TileMap, tilesToRemove[0]);
                    UndoManager.Record(action);
                }
                else
                {
                    var batchAction = new TileBatchAction(TileMap, new List<PlacedTile>(), tilesToRemove, "Remove Tiles");
                    UndoManager.Record(batchAction);
                }
            }

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
        private PlacedTile GetTileAt(int gridX, int gridY)
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

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            _currentRotation = (_currentRotation - 90 + 360) % 360;
            UpdateTransformPreview();
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            _currentRotation = (_currentRotation + 90) % 360;
            UpdateTransformPreview();
        }

        private void FlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            _currentFlipH = !_currentFlipH;
            UpdateTransformPreview();
        }

        private void FlipVertical_Click(object sender, RoutedEventArgs e)
        {
            _currentFlipV = !_currentFlipV;
            UpdateTransformPreview();
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

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void UpdateTransformPreview()
        {
            // Update visual feedback (highlight the active transform buttons)
            BtnRotateLeft.FontWeight = _currentRotation > 0 ? FontWeights.Bold : FontWeights.Normal;
            BtnRotateRight.FontWeight = _currentRotation > 0 ? FontWeights.Bold : FontWeights.Normal;
            BtnFlipH.FontWeight = _currentFlipH ? FontWeights.Bold : FontWeights.Normal;
            BtnFlipV.FontWeight = _currentFlipV ? FontWeights.Bold : FontWeights.Normal;
        }

        // Keyboard shortcuts
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            #region Ctrl+ Shortcuts

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.Z:
                        if (CanUndo)
                        {
                            Undo();
                            e.Handled = true;
                        }
                        break;
                    case Key.Y:
                        if (CanRedo)
                        {
                            Redo();
                            e.Handled = true;
                        }
                        break;
                    case Key.C:
                        CopySelection();
                        e.Handled = true;
                        break;
                    case Key.V:
                        PasteSelection();
                        e.Handled = true;
                        break;
                    case Key.X:
                        CutSelection();
                        e.Handled = true;
                        break;
                }
            }

            #endregion

            #region No Modifier Shortcuts

            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                switch (e.Key)
                {
                    // Tool shortcuts
                    case Key.B:
                        PaintMode_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.E:
                        EraseMode_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.P:
                        PropertiesMode_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.Delete:
                        // Delete tile at current position
                        DeleteTileAtCursor();
                        e.Handled = true;
                        break;

                    // Transform shortcuts
                    case Key.Q:
                        RotateLeft_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.W:
                        RotateRight_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.H:
                        FlipHorizontal_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.V:
                        FlipVertical_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.R:
                        // Reset transforms
                        _currentRotation = 0;
                        _currentFlipH = false;
                        _currentFlipV = false;
                        UpdateTransformPreview();
                        e.Handled = true;
                        break;

                    // DM View toggle
                    case Key.D:
                        ToggleDMView_Click(null, null);
                        e.Handled = true;
                        break;
                }
            }

            #endregion
        }

        /// <summary>
        /// Deletes tile at the current cursor position
        /// </summary>
        private void DeleteTileAtCursor()
        {
            if (TileMap == null) return;

            var mousePos = Mouse.GetPosition(MapCanvas);
            var gridPos = ScreenToGrid(mousePos);
            int gridX = gridPos.X;
            int gridY = gridPos.Y;

            if (gridX < 0 || gridX >= TileMap.Width || gridY < 0 || gridY >= TileMap.Height)
                return;

            // Find tiles at position on active layer
            var tilesToRemove = TileMap.PlacedTiles
                .Where(t => t.GridX == gridX && t.GridY == gridY)
                .ToList();

            if (tilesToRemove.Any())
            {
                foreach (var tile in tilesToRemove)
                {
                    var tileDef = TileLibraryService.Instance.GetTileById(tile.TileDefinitionId);
                    if (tileDef?.Layer == _activeLayer)
                    {
                        TileMap.PlacedTiles.Remove(tile);
                    }
                }
                RenderMap();
            }
        }

        #endregion

        #region Undo/Redo

        public void Undo()
        {
            _recordingUndo = false;
            UndoManager.Undo();
            RenderMap(); // Full re-render
            _recordingUndo = true;
        }

        public void Redo()
        {
            _recordingUndo = false;
            UndoManager.Redo();
            RenderMap(); // Full re-render
            _recordingUndo = true;
        }

        public bool CanUndo => UndoManager.CanUndo;
        public bool CanRedo => UndoManager.CanRedo;

        #endregion

        #region Helpers / Utility

        public Brush BackgroundBrush => new SolidColorBrush(TileMap?.BackgroundColor ?? Colors.DarkSlateGray);
        public string StatusText => $"Mode: {_currentMode} | Tiles: {TileMap?.PlacedTiles.Count ?? 0} | DM View: {(_showDMView ? "ON" : "OFF")}";

        private void CopySelection()
        {
            // For now, copy all tiles at selected position
            // TODO: Implement area selection with drag
            if (SelectedTileDefinition == null) return;

            var mousePos = Mouse.GetPosition(MapCanvas);
            var gridPos = ScreenToGrid(mousePos);

            var tiles = TileMap.GetTilesAt(gridPos.X, gridPos.Y).ToList();

            if (tiles.Count == 0)
            {
                AddToActionLog("Edit", "No tiles to copy");
                return;
            }

            _clipboard.Clear();
            _clipboardOrigin = gridPos;

            foreach (var tile in tiles)
            {
                // Clone the tile
                _clipboard.Add(new PlacedTile
                {
                    TileDefinitionId = tile.TileDefinitionId,
                    GridX = tile.GridX,
                    GridY = tile.GridY,
                    Rotation = tile.Rotation,
                    FlipHorizontal = tile.FlipHorizontal,
                    FlipVertical = tile.FlipVertical,
                    ZIndex = tile.ZIndex,
                    Notes = tile.Notes
                });
            }

            AddToActionLog("Edit", $"📋 Copied {_clipboard.Count} tiles");
        }

        private void PasteSelection()
        {
            if (_clipboard.Count == 0)
            {
                AddToActionLog("Edit", "Clipboard is empty");
                return;
            }

            var mousePos = Mouse.GetPosition(MapCanvas);
            var gridPos = ScreenToGrid(mousePos);

            var pastedTiles = new List<PlacedTile>();

            foreach (var clipTile in _clipboard)
            {
                // Calculate offset from original
                int offsetX = clipTile.GridX - _clipboardOrigin.X;
                int offsetY = clipTile.GridY - _clipboardOrigin.Y;

                var newTile = new PlacedTile
                {
                    TileDefinitionId = clipTile.TileDefinitionId,
                    GridX = gridPos.X + offsetX,
                    GridY = gridPos.Y + offsetY,
                    Rotation = clipTile.Rotation,
                    FlipHorizontal = clipTile.FlipHorizontal,
                    FlipVertical = clipTile.FlipVertical,
                    ZIndex = clipTile.ZIndex,
                    Notes = clipTile.Notes
                };

                // Check bounds
                if (newTile.GridX >= 0 && newTile.GridX < TileMap.Width &&
                    newTile.GridY >= 0 && newTile.GridY < TileMap.Height)
                {
                    TileMap.AddTile(newTile);
                    pastedTiles.Add(newTile);
                }
            }

            // Record undo
            if (_recordingUndo && pastedTiles.Count > 0)
            {
                var action = new TileBatchAction(TileMap, pastedTiles, new List<PlacedTile>(), "Paste Tiles");
                UndoManager.Record(action);
            }

            RenderMap();
            AddToActionLog("Edit", $"📋 Pasted {pastedTiles.Count} tiles");
        }

        private void CutSelection()
        {
            var mousePos = Mouse.GetPosition(MapCanvas);
            var gridPos = ScreenToGrid(mousePos);

            var tiles = TileMap.GetTilesAt(gridPos.X, gridPos.Y).ToList();

            if (tiles.Count == 0) return;

            // Copy to clipboard
            CopySelection();

            // Remove originals
            foreach (var tile in tiles)
            {
                TileMap.RemoveTile(tile);
            }

            // Record undo
            if (_recordingUndo)
            {
                var action = new TileBatchAction(TileMap, new List<PlacedTile>(), tiles, "Cut Tiles");
                UndoManager.Record(action);
            }

            RenderMap();
            AddToActionLog("Edit", $"✂️ Cut {tiles.Count} tiles");
        }

        // Helper to add action log (if not already present)
        private void AddToActionLog(string category, string message)
        {
            // Simple console output for now
            System.Diagnostics.Debug.WriteLine($"[{category}] {message}");
        }

        private Brush GetMetadataIconBackground(TileMetadataType type)
        {
            return type switch
            {
                TileMetadataType.Trap => new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)),
                TileMetadataType.Secret => new SolidColorBrush(Color.FromArgb(200, 255, 235, 59)),
                TileMetadataType.Interactive => new SolidColorBrush(Color.FromArgb(200, 33, 150, 243)),
                TileMetadataType.Hazard => new SolidColorBrush(Color.FromArgb(200, 255, 152, 0)),
                TileMetadataType.Teleporter => new SolidColorBrush(Color.FromArgb(200, 0, 188, 212)),
                TileMetadataType.Healing => new SolidColorBrush(Color.FromArgb(200, 76, 175, 80)),
                TileMetadataType.Spawn => new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)),
                _ => new SolidColorBrush(Color.FromArgb(200, 158, 158, 158))
            };
        }

        #endregion
    }
}