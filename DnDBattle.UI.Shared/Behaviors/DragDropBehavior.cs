using System.Windows;
using System.Windows.Input;

namespace DnDBattle.UI.Shared.Behaviors;

public static class DragDropBehavior
{
    public static readonly DependencyProperty EnableDragProperty =
        DependencyProperty.RegisterAttached("EnableDrag", typeof(bool), typeof(DragDropBehavior),
            new PropertyMetadata(false, OnEnableDragChanged));

    public static bool GetEnableDrag(DependencyObject obj) => (bool)obj.GetValue(EnableDragProperty);
    public static void SetEnableDrag(DependencyObject obj, bool value) => obj.SetValue(EnableDragProperty, value);

    private static void OnEnableDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        if ((bool)e.NewValue)
        {
            element.MouseLeftButtonDown += OnMouseDown;
            element.MouseMove += OnMouseMove;
            element.MouseLeftButtonUp += OnMouseUp;
        }
        else
        {
            element.MouseLeftButtonDown -= OnMouseDown;
            element.MouseMove -= OnMouseMove;
            element.MouseLeftButtonUp -= OnMouseUp;
        }
    }

    private static Point _startPoint;
    private static bool _isDragging;

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        _isDragging = false;
        ((UIElement)sender).CaptureMouse();
    }

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var pos = e.GetPosition(null);
        var diff = _startPoint - pos;
        if (!_isDragging && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                              Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            _isDragging = true;
            DragDrop.DoDragDrop((UIElement)sender, sender, DragDropEffects.Move);
        }
    }

    private static void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        ((UIElement)sender).ReleaseMouseCapture();
        _isDragging = false;
    }
}
