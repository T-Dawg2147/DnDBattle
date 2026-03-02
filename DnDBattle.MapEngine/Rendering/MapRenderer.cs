using DnDBattle.Core.Models;
using DnDBattle.MapEngine.Lighting;
using System.Windows;
using System.Windows.Media;

namespace DnDBattle.MapEngine.Rendering;

public sealed class MapRenderer
{
    private readonly LightingService _lighting;
    private double _cellSize = 50.0;

    public MapRenderer(LightingService lighting) => _lighting = lighting;

    public void Configure(double cellSize) => _cellSize = cellSize;

    public void RenderGrid(DrawingContext dc, int cols, int rows, Pen gridPen)
    {
        for (int c = 0; c <= cols; c++)
        {
            double x = c * _cellSize;
            dc.DrawLine(gridPen, new Point(x, 0), new Point(x, rows * _cellSize));
        }
        for (int r = 0; r <= rows; r++)
        {
            double y = r * _cellSize;
            dc.DrawLine(gridPen, new Point(0, y), new Point(cols * _cellSize, y));
        }
    }

    public void RenderToken(DrawingContext dc, Combatant combatant, bool isSelected, bool isActive)
    {
        var center = new Point(combatant.PositionX, combatant.PositionY);
        double radius = _cellSize * 0.4;

        var fill = combatant.IsPlayer
            ? new SolidColorBrush(Color.FromRgb(70, 130, 180))
            : new SolidColorBrush(Color.FromRgb(180, 60, 60));

        var outlinePen = isSelected
            ? new Pen(Brushes.Gold, 3)
            : isActive
                ? new Pen(Brushes.LimeGreen, 2)
                : new Pen(Brushes.DimGray, 1);

        dc.DrawEllipse(fill, outlinePen, center, radius, radius);

        // HP bar
        double hpFraction = combatant.MaxHitPoints > 0
            ? (double)combatant.CurrentHitPoints / combatant.MaxHitPoints : 0;
        var barBg = new Rect(center.X - radius, center.Y + radius + 2, radius * 2, 4);
        dc.DrawRectangle(Brushes.DarkRed, null, barBg);
        var barFg = new Rect(barBg.X, barBg.Y, barBg.Width * hpFraction, 4);
        dc.DrawRectangle(Brushes.LimeGreen, null, barFg);
    }

    public void RenderFogOverlay(DrawingContext dc, int cols, int rows,
        Func<int, int, bool> isRevealed)
    {
        var fogBrush = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
        for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows; r++)
                if (!isRevealed(c, r))
                    dc.DrawRectangle(fogBrush, null,
                        new Rect(c * _cellSize, r * _cellSize, _cellSize, _cellSize));
    }
}
