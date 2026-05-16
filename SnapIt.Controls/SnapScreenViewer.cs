using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Serilog;
using SnapIt.Common.Entities;
using SnapIt.Common.Extensions;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace SnapIt.Controls;

public class SnapScreenViewer : ListView
{
    //public SnapAreaTheme Theme { get; set; } = new SnapAreaTheme();

    public SnapScreenViewer()
    {
        Loaded += SnapScreenViewer_Loaded;
        SizeChanged += SnapScreenViewer_SizeChanged;
        ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
    }

    private void SnapScreenViewer_Loaded(object sender, RoutedEventArgs e)
    {
        AdoptToScreen();
    }

    private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
    {
        if (ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        {
            AdoptToScreen();
        }
    }

    protected override void OnChildDesiredSizeChanged(UIElement child)
    {
        base.OnChildDesiredSizeChanged(child);
        AdoptToScreen();
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
    }

    private void SnapScreenViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        AdoptToScreen();
    }

    private void AdoptToScreen()
    {
        Log.Logger.Information($"[AdoptToScreen] 被调用 - ItemCount: {Items.Count}, ActualWidth: {ActualWidth}, ActualHeight: {ActualHeight}");

        var borders = this.FindChildren<Border>("ItemBorder");

        Log.Logger.Information($"[AdoptToScreen] 找到 {borders.Count()} 个 ItemBorder");

        if (borders.Any())
        {
            var snapScreens = (IEnumerable<SnapScreen>)ItemsSource;

            if (snapScreens.Any() && ActualWidth != 0)
            {
                var maxScreenSizeX = snapScreens.Max(screen => screen.WorkingArea.X + screen.PhysicalBounds.Width);
                var maxScreenSizeY = snapScreens.Max(screen => screen.WorkingArea.Y + screen.PhysicalBounds.Height);

                var minScreenSizeX = Math.Abs(snapScreens.Min(screen => screen.WorkingArea.X));
                var minScreenSizeY = Math.Abs(snapScreens.Min(screen => screen.WorkingArea.Y));

                Log.Logger.Information($"[AdoptToScreen] maxScreenSizeX={maxScreenSizeX}, maxScreenSizeY={maxScreenSizeY}");
                Log.Logger.Information($"[AdoptToScreen] minScreenSizeX={minScreenSizeX}, minScreenSizeY={minScreenSizeY}");

                double factorX, factorY = 0.0;
                factorX = ActualWidth / (maxScreenSizeX + minScreenSizeX);
                factorY = ActualHeight / (maxScreenSizeY + minScreenSizeY);
                if (factorX > factorY)
                {
                    factorX = factorY;
                }
                else
                {
                    factorY = factorX;
                }

                const double minCardWidth = 140.0;
                const double minCardHeight = 250.0;
                var minPhysicalW = snapScreens.Min(screen => screen.PhysicalBounds.Width);
                var minPhysicalH = snapScreens.Min(screen => screen.PhysicalBounds.Height);

                if (minPhysicalW * factorX < minCardWidth || minPhysicalH * factorY < minCardHeight)
                {
                    var scaleX = minCardWidth / minPhysicalW;
                    var scaleY = minCardHeight / minPhysicalH;
                    var minFactor = Math.Max(scaleX, scaleY);
                    factorX = Math.Max(factorX, minFactor);
                    factorY = Math.Max(factorY, minFactor);
                }

                Log.Logger.Information($"[AdoptToScreen] factorX={factorX}, factorY={factorY}");

                Width = (maxScreenSizeX + minScreenSizeX) * factorX;
                Height = (maxScreenSizeY + minScreenSizeY) * factorY;

                foreach (var border in borders)
                {
                    var snapScreen = (SnapScreen)border.DataContext;

                    var posX = snapScreen.WorkingArea.X;
                    var posY = snapScreen.WorkingArea.Y;
                    var physW = snapScreen.PhysicalBounds.Width;
                    var physH = snapScreen.PhysicalBounds.Height;

                    var newPoint = new Point
                    {
                        X = (posX + minScreenSizeX) * factorX,
                        Y = (posY + minScreenSizeY) * factorY
                    };
                    var newSize = new Size
                    {
                        Width = physW * factorX,
                        Height = physH * factorY
                    };

                    Log.Logger.Information($"[AdoptToScreen] DeviceName={snapScreen.DeviceName}, posX={posX}, posY={posY}, physW={physW}, physH={physH}, newPoint=({newPoint.X},{newPoint.Y}), newSize={newSize.Width}x{newSize.Height}");

                    SetPos(border, newPoint, newSize);
                }
            }
        }
    }

    public void SetPos(Border border, Point point, Size size)
    {
        if (!point.X.Equals(double.NaN) && !point.Y.Equals(double.NaN))
        {
            border.Margin = new Thickness(point.X, point.Y, 0, 0);

            border.Width = size.Width;
            border.Height = size.Height;
        }
    }
}