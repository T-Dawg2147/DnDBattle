using System;
using System.Windows;
using System.Windows.Controls;

namespace DnDBattle.Views
{
    /// <summary>
    /// A panel wrapper that provides minimize, maximize, and float functionality.
    /// </summary>
    public partial class DockablePanel : UserControl
    {
        private UIElement? _child;
        private bool _isMinimized;
        private bool _isFloating;
        private Window? _floatingWindow;
        private GridLength _previousHeight;
        private GridLength _previousWidth;

        private const double MinFloatingWidth = 300;
        private const double MinFloatingHeight = 400;

        /// <summary>
        /// The title displayed in the panel header.
        /// </summary>
        public static readonly DependencyProperty PanelTitleProperty =
            DependencyProperty.Register(nameof(PanelTitle), typeof(string), typeof(DockablePanel),
                new PropertyMetadata("Panel", OnPanelTitleChanged));

        /// <summary>
        /// The content displayed inside the panel.
        /// </summary>
        public static readonly DependencyProperty PanelChildProperty =
            DependencyProperty.Register(nameof(PanelChild), typeof(UIElement), typeof(DockablePanel),
                new PropertyMetadata(null));

        public string PanelTitle
        {
            get => (string)GetValue(PanelTitleProperty);
            set => SetValue(PanelTitleProperty, value);
        }

        public UIElement? PanelChild
        {
            get => (UIElement?)GetValue(PanelChildProperty);
            set => SetValue(PanelChildProperty, value);
        }

        /// <summary>
        /// Fired when the panel is floated out into its own window.
        /// </summary>
        public event Action<DockablePanel>? PanelFloated;

        /// <summary>
        /// Fired when the panel is docked back from a floating window.
        /// </summary>
        public event Action<DockablePanel>? PanelDocked;

        /// <summary>
        /// Fired when the panel is minimized.
        /// </summary>
        public event Action<DockablePanel>? PanelMinimized;

        /// <summary>
        /// Fired when the panel is restored from minimized state.
        /// </summary>
        public event Action<DockablePanel>? PanelRestored;

        public bool IsMinimized => _isMinimized;
        public bool IsFloating => _isFloating;

        public DockablePanel()
        {
            InitializeComponent();
        }

        private static void OnPanelTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DockablePanel panel)
            {
                panel.TitleText.Text = e.NewValue as string ?? "Panel";
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMinimize();
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void BtnFloat_Click(object sender, RoutedEventArgs e)
        {
            ToggleFloat();
        }

        /// <summary>
        /// Toggles the minimized state of the panel content.
        /// </summary>
        public void ToggleMinimize()
        {
            _isMinimized = !_isMinimized;

            if (_isMinimized)
            {
                PanelContent.Visibility = Visibility.Collapsed;
                BtnMinimize.Content = "🗗";
                BtnMinimize.ToolTip = "Restore";
                PanelMinimized?.Invoke(this);
            }
            else
            {
                PanelContent.Visibility = Visibility.Visible;
                BtnMinimize.Content = "🗕";
                BtnMinimize.ToolTip = "Minimize";
                PanelRestored?.Invoke(this);
            }
        }

        /// <summary>
        /// Toggles the maximized state - expands the panel or restores it.
        /// </summary>
        public void ToggleMaximize()
        {
            // Find the parent RowDefinition or ColumnDefinition and toggle between * and saved size
            var parent = this.Parent as FrameworkElement;
            if (parent == null) return;

            var parentGrid = parent.Parent as Grid ?? parent as Grid;
            if (parentGrid == null) return;

            int row = Grid.GetRow(parent is Grid ? this : parent);
            int col = Grid.GetColumn(parent is Grid ? this : parent);

            if (parentGrid.RowDefinitions.Count > row && row >= 0)
            {
                var rowDef = parentGrid.RowDefinitions[row];
                if (rowDef.Height == new GridLength(1, GridUnitType.Star))
                {
                    // Restore
                    if (_previousHeight != default)
                        rowDef.Height = _previousHeight;
                }
                else
                {
                    _previousHeight = rowDef.Height;
                    rowDef.Height = new GridLength(1, GridUnitType.Star);
                }
            }

            if (parentGrid.ColumnDefinitions.Count > col && col >= 0)
            {
                var colDef = parentGrid.ColumnDefinitions[col];
                if (colDef.Width == new GridLength(1, GridUnitType.Star))
                {
                    if (_previousWidth != default)
                        colDef.Width = _previousWidth;
                }
                else
                {
                    _previousWidth = colDef.Width;
                    colDef.Width = new GridLength(1, GridUnitType.Star);
                }
            }
        }

        /// <summary>
        /// Toggles between floating (detached window) and docked state.
        /// </summary>
        public void ToggleFloat()
        {
            if (_isFloating)
            {
                DockPanel();
            }
            else
            {
                FloatPanel();
            }
        }

        private void FloatPanel()
        {
            if (_isFloating) return;

            _child = PanelChild;
            var mainWindow = Window.GetWindow(this);

            _floatingWindow = new Window
            {
                Title = PanelTitle,
                Width = Math.Max(this.ActualWidth, MinFloatingWidth),
                Height = Math.Max(this.ActualHeight, MinFloatingHeight),
                Background = System.Windows.Media.Brushes.Transparent,
                AllowsTransparency = false,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = mainWindow
            };

            // Remove child from this panel
            PanelChild = null;

            // Create a wrapper with dark background for the floating window
            var wrapper = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E")),
                Child = _child
            };

            _floatingWindow.Content = wrapper;
            _floatingWindow.Closed += FloatingWindow_Closed;

            _isFloating = true;
            BtnFloat.Content = "⧈";
            BtnFloat.ToolTip = "Dock Panel";
            this.Visibility = Visibility.Collapsed;

            _floatingWindow.Show();
            PanelFloated?.Invoke(this);
        }

        private void DockPanel()
        {
            if (!_isFloating || _floatingWindow == null) return;

            _floatingWindow.Closed -= FloatingWindow_Closed;
            _floatingWindow.Close();

            RestoreDockedState();
        }

        private void FloatingWindow_Closed(object? sender, EventArgs e)
        {
            if (_isFloating)
            {
                RestoreDockedState();
            }
        }

        private void RestoreDockedState()
        {
            // Get child back from the floating window
            if (_floatingWindow?.Content is Border wrapper)
            {
                wrapper.Child = null;
                PanelChild = _child;
            }

            _floatingWindow = null;
            _isFloating = false;
            BtnFloat.Content = "⧉";
            BtnFloat.ToolTip = "Float Panel";
            this.Visibility = Visibility.Visible;

            PanelDocked?.Invoke(this);
        }
    }
}
