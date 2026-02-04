using DnDBattle.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    /// <summary>
    /// Manages token visuals, movement, and interactions
    /// </summary>
    public class BattleGridTokenManager
    {
        #region Fields

        private readonly Canvas _renderCanvas;
        private ObservableCollection<Token> _tokens;

        // Dragging state
        private bool _isDragging;
        private FrameworkElement _draggingElement;
        private Token _draggingToken;
        private Point _dragStartPoint;
        private int _dragStartGridX;
        private int _dragStartGridY;

        private Token _selectedToken;

        #endregion

        #region Events

        public event Action<Token> TokenClicked;
        public event Action<Token> TokenDoubleClicked;
        public event Action<Token, int, int, int, int> TokenMoved; // token, oldX, oldY, newX, newY
        public event System.Action StopPanning;
        public event Action<string, string> LogMessage;

        #endregion

        #region Properties

        public bool IsDragging => _isDragging;

        #endregion

        #region Constructor

        public BattleGridTokenManager(Canvas renderCanvas)
        {
            _renderCanvas = renderCanvas ?? throw new ArgumentNullException(nameof(renderCanvas));
            System.Diagnostics.Debug.WriteLine("[TokenManager] Initialized");
        }

        #endregion

        #region Public Methods - Setup

        public void SetTokens(ObservableCollection<Token> tokens)
        {
            // Unsubscribe from old collection
            if (_tokens != null)
            {
                _tokens.CollectionChanged -= Tokens_CollectionChanged;
            }

            _tokens = tokens;

            // Subscribe to new collection
            if (_tokens != null)
            {
                _tokens.CollectionChanged += Tokens_CollectionChanged;
            }

            System.Diagnostics.Debug.WriteLine($"[TokenManager] Tokens collection set: {_tokens?.Count ?? 0} tokens");
        }

        public void SelectToken(Token token)
        {
            _selectedToken = token;
            // TODO: Add visual selection indicator
        }

        #endregion

        #region Public Methods - Rendering

        /// <summary>
        /// Rebuilds all token visuals from scratch
        /// </summary>
        public void RebuildAllTokenVisuals(double cellSize)
        {
            System.Diagnostics.Debug.WriteLine("[TokenManager] Rebuilding all token visuals...");

            // Remove existing token visuals
            var existingTokens = _renderCanvas.Children.OfType<FrameworkElement>()
                .Where(e => e.Tag is Token)
                .ToList();

            foreach (var element in existingTokens)
            {
                _renderCanvas.Children.Remove(element);
            }

            if (_tokens == null)
            {
                System.Diagnostics.Debug.WriteLine("[TokenManager] No tokens to render");
                return;
            }

            // Create new visuals
            foreach (var token in _tokens)
            {
                CreateTokenVisual(token, cellSize);
            }

            System.Diagnostics.Debug.WriteLine($"[TokenManager] Rendered {_tokens.Count} tokens");
        }

        /// <summary>
        /// Updates token positions without rebuilding
        /// </summary>
        public void UpdateTokenPositions(double cellSize)
        {
            if (_tokens == null) return;

            foreach (var token in _tokens)
            {
                var element = FindTokenVisual(token);
                if (element != null)
                {
                    Canvas.SetLeft(element, token.GridX * cellSize);
                    Canvas.SetTop(element, token.GridY * cellSize);
                }
            }
        }

        #endregion

        #region Public Methods - Input Handling

        public void HandleMouseDown(Point position, Transform transform, double cellSize)
        {
            var worldPos = transform.Inverse?.Transform(position) ?? position;

            // Find clicked token
            var clickedToken = FindTokenAt(worldPos, cellSize);

            if (clickedToken != null)
            {
                _draggingToken = clickedToken;
                _draggingElement = FindTokenVisual(clickedToken);
                _dragStartPoint = position;
                _dragStartGridX = clickedToken.GridX;
                _dragStartGridY = clickedToken.GridY;
                _isDragging = true;

                _draggingElement?.CaptureMouse();

                // Tell input manager to stop panning
                StopPanning?.Invoke(); // ← NEW

                TokenClicked?.Invoke(clickedToken);
                System.Diagnostics.Debug.WriteLine($"[TokenManager] Started dragging: {clickedToken.Name}");
            }
        }

        public void HandleMouseMove(Point position, Transform transform, double cellSize, bool lockToGrid)
        {
            if (!_isDragging || _draggingElement == null || _draggingToken == null) return;

            var delta = position - _dragStartPoint;

            var currentLeft = Canvas.GetLeft(_draggingElement);
            var currentTop = Canvas.GetTop(_draggingElement);

            Canvas.SetLeft(_draggingElement, currentLeft + delta.X);
            Canvas.SetTop(_draggingElement, currentTop + delta.Y);

            _dragStartPoint = position;
        }

        public void HandleMouseUp(Point position, Transform transform, double cellSize, bool lockToGrid)
        {
            if (!_isDragging || _draggingElement == null || _draggingToken == null) return;

            _draggingElement.ReleaseMouseCapture();

            var left = Canvas.GetLeft(_draggingElement);
            var top = Canvas.GetTop(_draggingElement);

            // Convert to grid coordinates
            int newGridX, newGridY;

            if (lockToGrid)
            {
                newGridX = (int)Math.Round(left / cellSize);
                newGridY = (int)Math.Round(top / cellSize);
            }
            else
            {
                newGridX = (int)Math.Floor(left / cellSize);
                newGridY = (int)Math.Floor(top / cellSize);
            }

            // Snap to grid
            Canvas.SetLeft(_draggingElement, newGridX * cellSize);
            Canvas.SetTop(_draggingElement, newGridY * cellSize);

            // Update token model
            int oldX = _dragStartGridX;
            int oldY = _dragStartGridY;

            _draggingToken.GridX = newGridX;
            _draggingToken.GridY = newGridY;

            // Fire moved event if position changed
            if (newGridX != oldX || newGridY != oldY)
            {
                TokenMoved?.Invoke(_draggingToken, oldX, oldY, newGridX, newGridY);
                LogMessage?.Invoke("Movement", $"{_draggingToken.Name} moved to ({newGridX}, {newGridY})");
            }

            _isDragging = false;
            _draggingElement = null;
            _draggingToken = null;

            System.Diagnostics.Debug.WriteLine($"[TokenManager] Drop complete at ({newGridX}, {newGridY})");
        }

        #endregion

        #region Private Methods - Visual Creation

        private void CreateTokenVisual(Token token, double cellSize)
        {
            try
            {
                double size = cellSize * token.SizeInSquares;

                var container = new Grid
                {
                    Width = size,
                    Height = size,
                    Tag = token,
                    Background = Brushes.Transparent
                };

                // USE BACKUP IMAGE if no image provided
                ImageSource imageSource = token.Image ?? CreateBackupTokenImage(token, size);

                var image = new Image
                {
                    Width = size,
                    Height = size,
                    Source = imageSource,
                    Stretch = System.Windows.Media.Stretch.UniformToFill
                };

                // Clip to circle
                var clip = new EllipseGeometry(new Point(size / 2, size / 2), size / 2, size / 2);
                image.Clip = clip;

                container.Children.Add(image);

                // Position
                Canvas.SetLeft(container, token.GridX * cellSize);
                Canvas.SetTop(container, token.GridY * cellSize);
                Canvas.SetZIndex(container, 100);

                // Events
                container.MouseLeftButtonDown += TokenVisual_MouseDown;

                _renderCanvas.Children.Add(container);

                System.Diagnostics.Debug.WriteLine($"[TokenManager] Created visual for {token.Name} at ({token.GridX}, {token.GridY})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TokenManager] Error creating token visual: {ex.Message}");
            }
        }

        private void TokenVisual_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Token token)
            {
                if (e.ClickCount == 2)
                {
                    TokenDoubleClicked?.Invoke(token);
                    e.Handled = true;
                }
            }
        }

        private ImageSource CreateBackupTokenImage(Token token, double size)
        {
            try
            {
                // Get initials (first letter of each word, max 2)
                string initials = GetInitials(token.Name);

                // Choose background color based on token type
                Color bgColor = token.IsPlayer ? Colors.RoyalBlue : Colors.Firebrick;

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    // Draw circle background
                    var bgBrush = new RadialGradientBrush(
                        Color.FromRgb((byte)(bgColor.R * 0.8), (byte)(bgColor.G * 0.8), (byte)(bgColor.B * 0.8)),
                        bgColor);
                    bgBrush.Freeze();

                    var borderPen = new Pen(Brushes.White, 3);
                    borderPen.Freeze();

                    dc.DrawEllipse(bgBrush, borderPen, new Point(size / 2, size / 2), size / 2 - 2, size / 2 - 2);

                    // Draw initials
                    var typeface = new Typeface(new System.Windows.Media.FontFamily("Segoe UI"),
                                               FontStyles.Normal,
                                               FontWeights.Bold,
                                               FontStretches.Normal);

                    var formattedText = new FormattedText(
                        initials,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        size * 0.4,
                        Brushes.White,
                        VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                    var textX = (size - formattedText.Width) / 2;
                    var textY = (size - formattedText.Height) / 2;

                    dc.DrawText(formattedText, new Point(textX, textY));
                }

                // Render to bitmap
                var rtb = new RenderTargetBitmap((int)size, (int)size, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);
                rtb.Freeze();

                return rtb;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TokenManager] Error creating backup image: {ex.Message}");
                return CreateFallbackImage();
            }
        }

        private ImageSource CreateFallbackImage()
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var brush = new RadialGradientBrush(Colors.CornflowerBlue, Colors.RoyalBlue);
                brush.Freeze();
                dc.DrawEllipse(brush, new Pen(Brushes.White, 2), new Point(24, 24), 22, 22);
            }

            var rtb = new RenderTargetBitmap(48, 48, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            return rtb;
        }

        #endregion

        #region Private Methods - Helpers

        private Token FindTokenAt(Point worldPosition, double cellSize)
        {
            if (_tokens == null) return null;

            // Check tokens in reverse render order (top tokens first)
            foreach (var token in _tokens.Reverse())
            {
                double tokenLeft = token.GridX * cellSize;
                double tokenTop = token.GridY * cellSize;
                double tokenSize = cellSize * token.SizeInSquares;

                var tokenRect = new Rect(tokenLeft, tokenTop, tokenSize, tokenSize);

                if (tokenRect.Contains(worldPosition))
                {
                    return token;
                }
            }

            return null;
        }

        private FrameworkElement FindTokenVisual(Token token)
        {
            return _renderCanvas.Children.OfType<FrameworkElement>()
                .FirstOrDefault(e => e.Tag is Token t && t.Id == token.Id);
        }

        private void Tokens_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[TokenManager] Collection changed: {e.Action}");

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                // Rebuild only new tokens
                foreach (Token token in e.NewItems)
                {
                    CreateTokenVisual(token, 48); // Use current cell size
                    System.Diagnostics.Debug.WriteLine($"[TokenManager] Added visual for new token: {token.Name}");
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                // Remove token visuals
                foreach (Token token in e.OldItems)
                {
                    var visual = FindTokenVisual(token);
                    if (visual != null)
                    {
                        _renderCanvas.Children.Remove(visual);
                        System.Diagnostics.Debug.WriteLine($"[TokenManager] Removed visual for token: {token.Name}");
                    }
                }
            }
            else
            {
                // Full rebuild for other changes
                System.Diagnostics.Debug.WriteLine($"[TokenManager] Full rebuild triggered");
            }
        }

        /// <summary>
        /// Gets initials from a name (max 2 characters)
        /// </summary>
        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "??";

            var words = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return "??";

            if (words.Length == 1)
                return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpper();

            // Take first letter of first two words
            return (words[0][0].ToString() + words[1][0].ToString()).ToUpper();
        }

        #endregion
    }
}