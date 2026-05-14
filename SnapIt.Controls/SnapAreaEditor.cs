﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SnapIt.Common.Entities;
using SnapIt.Common.Extensions;
using Wpf.Ui.Input;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace SnapIt.Controls;

public class SnapAreaEditor : Control
{
    public SnapControl SnapControl { get; set; }

    public Thickness AreaPadding
    {
        get => (Thickness)GetValue(AreaPaddingProperty);
        set => SetValue(AreaPaddingProperty, value);
    }

    public static readonly DependencyProperty AreaPaddingProperty
     = DependencyProperty.Register("AreaPadding", typeof(Thickness), typeof(SnapAreaEditor),
       new FrameworkPropertyMetadata()
       {
           DefaultValue = new Thickness(0),
           BindsTwoWayByDefault = true,
           PropertyChangedCallback = new PropertyChangedCallback(AreaPaddingPropertyChanged)
       });

    private static void AreaPaddingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var snapAreaEditor = (SnapAreaEditor)d;
        snapAreaEditor.AreaPadding = (Thickness)e.NewValue;
    }

    public bool IsAreaMouseOver
    {
        get => (bool)GetValue(IsAreaMouseOverProperty);
        set => SetValue(IsAreaMouseOverProperty, value);
    }

    public static readonly DependencyProperty IsAreaMouseOverProperty = DependencyProperty.Register("IsAreaMouseOver",
        typeof(bool), typeof(SnapAreaEditor), new PropertyMetadata(null));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected",
        typeof(bool), typeof(SnapAreaEditor), new PropertyMetadata(false));

    public SnapAreaTheme Theme
    {
        get => (SnapAreaTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public static readonly DependencyProperty ThemeProperty
     = DependencyProperty.Register("Theme", typeof(SnapAreaTheme), typeof(SnapAreaEditor),
       new FrameworkPropertyMetadata()
       {
           BindsTwoWayByDefault = true,
           PropertyChangedCallback = new PropertyChangedCallback(ThemePropertyChanged)
       });

    private static void ThemePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var SnapAreaEditor = (SnapAreaEditor)d;
        SnapAreaEditor.Theme = (SnapAreaTheme)e.NewValue;

        //if (SnapAreaEditor.Theme != null)
        //{
        //    SnapAreaEditor.Area.Opacity = SnapAreaEditor.Theme.Opacity;
        //    SnapAreaEditor.Area.Background = SnapAreaEditor.Theme.OverlayBrush;
        //}
    }

    public static readonly DependencyProperty SplitVerticallyCommandProperty =
        DependencyProperty.Register("SplitVerticallyCommand",
            typeof(IRelayCommand), typeof(SnapAreaEditor), new PropertyMetadata(null));

    private IRelayCommand SplitVerticallyCommand => (IRelayCommand)GetValue(SplitVerticallyCommandProperty);

    public static readonly DependencyProperty SplitHorizantallyCommandProperty =
        DependencyProperty.Register("SplitHorizantallyCommand",
            typeof(IRelayCommand), typeof(SnapAreaEditor), new PropertyMetadata(null));

    public IRelayCommand SplitHorizantallyCommand => (IRelayCommand)GetValue(SplitHorizantallyCommandProperty);

    public int VerticalDivideCount
    {
        get => (int)GetValue(VerticalDivideCountProperty);
        set => SetValue(VerticalDivideCountProperty, value);
    }

    public static readonly DependencyProperty VerticalDivideCountProperty =
        DependencyProperty.Register("VerticalDivideCount", typeof(int), typeof(SnapAreaEditor),
            new PropertyMetadata(3));

    public int HorizontalDivideCount
    {
        get => (int)GetValue(HorizontalDivideCountProperty);
        set => SetValue(HorizontalDivideCountProperty, value);
    }

    public static readonly DependencyProperty HorizontalDivideCountProperty =
        DependencyProperty.Register("HorizontalDivideCount", typeof(int), typeof(SnapAreaEditor),
            new PropertyMetadata(3));

    public static readonly DependencyProperty SplitEqualVerticallyCommandProperty =
        DependencyProperty.Register("SplitEqualVerticallyCommand",
            typeof(IRelayCommand), typeof(SnapAreaEditor), new PropertyMetadata(null));

    public IRelayCommand SplitEqualVerticallyCommand => (IRelayCommand)GetValue(SplitEqualVerticallyCommandProperty);

    public static readonly DependencyProperty SplitEqualHorizontallyCommandProperty =
        DependencyProperty.Register("SplitEqualHorizontallyCommand",
            typeof(IRelayCommand), typeof(SnapAreaEditor), new PropertyMetadata(null));

    public IRelayCommand SplitEqualHorizontallyCommand => (IRelayCommand)GetValue(SplitEqualHorizontallyCommandProperty);

    public SnapAreaEditor()
    {
        SetValue(SplitVerticallyCommandProperty,
            new RelayCommand<object>(o =>
            {
                Split(SplitDirection.Vertical);
            }));

        SetValue(SplitHorizantallyCommandProperty,
            new RelayCommand<object>(o =>
            {
                Split(SplitDirection.Horizontal);
            }));

        SetValue(SplitEqualVerticallyCommandProperty,
            new RelayCommand<object>(o =>
            {
                Split(SplitDirection.Vertical, VerticalDivideCount);
            }));

        SetValue(SplitEqualHorizontallyCommandProperty,
            new RelayCommand<object>(o =>
            {
                Split(SplitDirection.Horizontal, HorizontalDivideCount);
            }));

        Loaded += SnapAreaEditor_Loaded;
        MouseLeftButtonDown += SnapAreaEditor_MouseLeftButtonDown;
    }

    private void SnapAreaEditor_Loaded(object sender, RoutedEventArgs e)
    {
        var area = this.FindChild<Grid>("Area");
        if (area != null)
        {
            area.IsMouseDirectlyOverChanged += SnapAreaEditor_IsMouseDirectlyOverChanged;
        }

        var splitVerticalEqual = this.FindChild<FrameworkElement>("SplitVerticalEqual");
        if (splitVerticalEqual != null)
        {
            splitVerticalEqual.MouseWheel += SplitVerticalEqual_MouseWheel;
        }

        var splitHorizontalEqual = this.FindChild<FrameworkElement>("SplitHorizontalEqual");
        if (splitHorizontalEqual != null)
        {
            splitHorizontalEqual.MouseWheel += SplitHorizontalEqual_MouseWheel;
        }
    }

    private void SplitVerticalEqual_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        VerticalDivideCount = Math.Clamp(VerticalDivideCount + (e.Delta > 0 ? 1 : -1), 3, 10);
        e.Handled = true;
    }

    private void SplitHorizontalEqual_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        HorizontalDivideCount = Math.Clamp(HorizontalDivideCount + (e.Delta > 0 ? 1 : -1), 3, 10);
        e.Handled = true;
    }

    private void SnapAreaEditor_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        IsAreaMouseOver = IsMouseOver;
    }

    private void SnapAreaEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (SnapControl.IsAreaSelectionMode)
        {
            SnapControl.ToggleAreaSelection(this);
            e.Handled = true;
        }
    }

    private void Split(SplitDirection direction, int divideCount = 2)
    {
        var rect = GetRect();

        if (divideCount < 2) return;

        for (int i = 1; i < divideCount; i++)
        {
            Point point;
            Size size;

            double ratio = (double)i / divideCount;

            if (direction == SplitDirection.Vertical)
            {
                point = new Point(rect.TopLeft.X + rect.Width * ratio, rect.TopLeft.Y);
                size = new Size(double.NaN, rect.Height);
            }
            else
            {
                point = new Point(rect.TopLeft.X, rect.TopLeft.Y + rect.Height * ratio);
                size = new Size(rect.Width, double.NaN);
            }

            var newBorder = new SnapBorder(SnapControl, new SnapAreaTheme());
            newBorder.SetPos(point, size, direction);

            SnapControl.AddBorder(newBorder);
        }
    }

    public Rect GetRect()
    {
        return new Rect(
            new Point(Margin.Left, Margin.Top),
            new Size(
                ActualWidth == 0 ? Width : ActualWidth,
                ActualHeight == 0 ? Height : ActualHeight));
    }
}