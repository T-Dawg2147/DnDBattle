using DnDBattle.Models;
using DnDBattle.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DnDBattle.Controls.BattleGrid.Managers
{
    public class BattleGridTokenManager
    {
        #region Fields

        private readonly Canvas _renderCanvas;
        private ObservableCollection<Token> _tokens;

        #endregion

        #region Events

        public event Action<Token> TokenClicked;
        public event Action<Token> TokenDoubleClicked;
        public event Action<Token, int, int, int, int> TokenMoved;
        public event Action<string, string> LogMessage;

        #endregion

        #region Constructor

        public BattleGridTokenManager(Canvas renderCanvas)
        {
            _renderCanvas = renderCanvas ?? throw new ArgumentNullException(nameof(renderCanvas));
            Debug.WriteLine("[TokenManager] Initialized");
        }

        #endregion

        #region Public Methods

        public void SetTokens(ObservableCollection<Token> tokens)
        {
            if (_tokens != null)
            {
                _tokens.CollectionChanged -= Tokens_CollectionChanged;
            }

            _tokens = tokens;

            if (_tokens != null)
            {
                _tokens.CollectionChanged += Tokens_CollectionChanged;
            }

            Debug.WriteLine($"[TokenManager] Tokens collection set: {_tokens?.Count ?? 0} tokens");
        }

        public void RebuildAllTokenVisuals(double cellSize)
        {
            ClearAllTokenVisuals();

            if (_tokens == null) return;

            foreach (var token in _tokens)
            {
                CreateTokenVisual(token, cellSize);
            }

            Debug.WriteLine($"[TokenManager] Rebuilt {_tokens.Count} token visuals");
        }

        public void UpdateTokenPositions(double cellSize)
        {
            if (_tokens == null) return;

            foreach (UIElement child in _renderCanvas.Children)
            {
                if (child is FrameworkElement element && element.Tag is Token token)
                {
                    Canvas.SetLeft(element, token.GridX * cellSize);
                    Canvas.SetTop(element, token.GridY * cellSize);

                    double size = cellSize * token.SizeInSquares;
                    element.Width = size;
                    element.Height = size;
                }
            }
        }

        #endregion

        #region Private Methods

        private void Tokens_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine($"[TokenManager] Collection changed: {e.Action}");

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (Token token in e.NewItems)
                {
                    CreateTokenVisual(token, 48);
                    Debug.WriteLine($"[TokenManager] Added visual for new token: {token.Name}");
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (Token token in e.OldItems)
                {
                    RemoveTokenVisual(token);
                    Debug.WriteLine($"[TokenManager] Removed visual for token: {token.Name}");
                }
            }
        }

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

                // Use CreatureImageService for placeholder tokens
                ImageSource imageSource = token.Image ??
                    CreatureImageService.GeneratePlaceholderToken(
                        token.Name,
                        token.Type ?? "Unknown",
                        token.Size ?? "Medium",
                        token.ChallengeRating?.ToString() ?? "0"
                    );

                var image = new Image
                {
                    Width = size,
                    Height = size,
                    Source = imageSource,
                    Stretch = Stretch.UniformToFill
                };

                var clip = new EllipseGeometry(new Point(size / 2, size / 2), size / 2, size / 2);
                image.Clip = clip;

                container.Children.Add(image);

                Canvas.SetLeft(container, token.GridX * cellSize);
                Canvas.SetTop(container, token.GridY * cellSize);
                Canvas.SetZIndex(container, 100);

                _renderCanvas.Children.Add(container);

                Debug.WriteLine($"[TokenManager] Created visual for {token.Name} at ({token.GridX}, {token.GridY})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TokenManager] Error creating token visual: {ex.Message}");
            }
        }

        private void RemoveTokenVisual(Token token)
        {
            for (int i = _renderCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (_renderCanvas.Children[i] is FrameworkElement element && element.Tag == token)
                {
                    _renderCanvas.Children.RemoveAt(i);
                    return;
                }
            }
        }

        private void ClearAllTokenVisuals()
        {
            for (int i = _renderCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (_renderCanvas.Children[i] is FrameworkElement element && element.Tag is Token)
                {
                    _renderCanvas.Children.RemoveAt(i);
                }
            }
        }

        #endregion
    }
}