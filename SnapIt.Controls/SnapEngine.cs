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
    private List<double> snapCentersX = [];
    private List<double> snapCentersY = [];

    public void BuildSnapLines(SnapControl snapControl)
    {
        snapLinesX = [];
        snapLinesY = [];
        snapCentersX = [];
        snapCentersY = [];

        var gridWidth = snapControl.MainGrid.ActualWidth;
        var gridHeight = snapControl.MainGrid.ActualHeight;

        if (gridWidth <= 0 || gridHeight <= 0) return;

        snapLinesX.Add(0);
        snapLinesX.Add(gridWidth);
        snapLinesY.Add(0);
        snapLinesY.Add(gridHeight);

        snapCentersX.Add(gridWidth / 2);
        snapCentersY.Add(gridHeight / 2);

        foreach (var border in snapControl.FindChildren<SnapBorder>().Where(b => b.IsDraggable))
        {
            if (border.SplitDirection == SplitDirection.Vertical)
            {
                var lineX = border.Margin.Left + SnapBorder.THICKNESSHALF;
                snapLinesX.Add(lineX);
                snapCentersX.Add(lineX);
            }
            else
            {
                var lineY = border.Margin.Top + SnapBorder.THICKNESSHALF;
                snapLinesY.Add(lineY);
                snapCentersY.Add(lineY);
            }
        }

        foreach (var area in snapControl.FindChildren<SnapAreaEditor>())
        {
            var left = area.Margin.Left;
            var right = left + area.Width;
            var top = area.Margin.Top;
            var bottom = top + area.Height;
            var centerX = (left + right) / 2;
            var centerY = (top + bottom) / 2;

            snapLinesX.Add(left);
            snapLinesX.Add(right);
            snapLinesY.Add(top);
            snapLinesY.Add(bottom);

            snapCentersX.Add(centerX);
            snapCentersY.Add(centerY);

            snapCentersX.Add(left);
            snapCentersX.Add(right);
            snapCentersY.Add(top);
            snapCentersY.Add(bottom);
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

    public (double x, double y) SnapCenter(double cx, double cy)
    {
        return (SnapValue(cx, snapCentersX), SnapValue(cy, snapCentersY));
    }

    public (double x, double y, double w, double h) SnapRectCenter(double x, double y, double w, double h)
    {
        var centerX = x + w / 2;
        var centerY = y + h / 2;

        var (snappedCX, snappedCY) = SnapCenter(centerX, centerY);

        return (
            snappedCX - w / 2,
            snappedCY - h / 2,
            w,
            h
        );
    }
}
