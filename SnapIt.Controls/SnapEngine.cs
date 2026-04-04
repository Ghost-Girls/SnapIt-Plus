using System.Windows;
using SnapIt.Common.Entities;
using SnapIt.Common.Extensions;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace SnapIt.Controls;

public class SnapEngine
{
    public const double SNAP_THRESHOLD = 8;

    private List<double> snapLinesX = [];
    private List<double> snapLinesY = [];

    public void BuildSnapLines(SnapControl snapControl)
    {
        snapLinesX = [];
        snapLinesY = [];

        var gridWidth = snapControl.MainGrid.ActualWidth;
        var gridHeight = snapControl.MainGrid.ActualHeight;

        if (gridWidth <= 0 || gridHeight <= 0) return;

        snapLinesX.Add(0);
        snapLinesX.Add(gridWidth);
        snapLinesY.Add(0);
        snapLinesY.Add(gridHeight);

        foreach (var border in snapControl.FindChildren<SnapBorder>().Where(b => b.IsDraggable))
        {
            if (border.SplitDirection == SplitDirection.Vertical)
                snapLinesX.Add(border.Margin.Left + SnapBorder.THICKNESSHALF);
            else
                snapLinesY.Add(border.Margin.Top + SnapBorder.THICKNESSHALF);
        }

        foreach (var area in snapControl.FindChildren<SnapAreaEditor>())
        {
            var left = area.Margin.Left;
            var right = left + area.Width;
            var top = area.Margin.Top;
            var bottom = top + area.Height;

            snapLinesX.Add(left);
            snapLinesX.Add(right);
            snapLinesY.Add(top);
            snapLinesY.Add(bottom);
        }
    }

    public double SnapValue(double value, List<double> snapLines)
    {
        foreach (var line in snapLines)
        {
            if (Math.Abs(value - line) < SNAP_THRESHOLD)
                return line;
        }
        return value;
    }

    public (double x, double y, double w, double h) SnapRect(double x, double y, double w, double h)
    {
        var snappedLeft = SnapValue(x, snapLinesX);
        var snappedTop = SnapValue(y, snapLinesY);
        var snappedRight = SnapValue(x + w, snapLinesX);
        var snappedBottom = SnapValue(y + h, snapLinesY);

        return (
            snappedLeft,
            snappedTop,
            snappedRight - snappedLeft,
            snappedBottom - snappedTop
        );
    }
}
