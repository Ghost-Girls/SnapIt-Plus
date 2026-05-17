using System.Windows;
using SnapIt.Common;
using SnapIt.Common.Contracts;
using SnapIt.Common.Entities;
using SnapIt.ViewModels.Windows;

namespace SnapIt.Views.Windows;

public partial class DesignWindow : IWindow
{
    public DesignWindowViewModel ViewModel { get; }

    public DesignWindow(
        DesignWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

        MouseMove += DesignWindow_MouseMove;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ViewModel?.SizeToWorkingArea();
    }

    private void DesignWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        Point myPoint = e.GetPosition(this);
        mousePositionText.Text = $"Mouse Position  {myPoint.X:0.00} x {myPoint.Y:0.00}";

        horizontalRuler.RaiseHorizontalRulerMoveEvent(e);
        verticalRuler.RaiseVerticalRulerMoveEvent(e);
    }

    private void DockTopLeft_Click(object sender, RoutedEventArgs e)
    {
        PropertiesPanel.HorizontalAlignment = HorizontalAlignment.Left;
        PropertiesPanel.VerticalAlignment = VerticalAlignment.Top;
        PropertiesPanel.Margin = new Thickness(20, 20, 0, 0);
    }

    private void DockTopRight_Click(object sender, RoutedEventArgs e)
    {
        PropertiesPanel.HorizontalAlignment = HorizontalAlignment.Right;
        PropertiesPanel.VerticalAlignment = VerticalAlignment.Top;
        PropertiesPanel.Margin = new Thickness(0, 20, 20, 0);
    }

    private void DockBottomLeft_Click(object sender, RoutedEventArgs e)
    {
        PropertiesPanel.HorizontalAlignment = HorizontalAlignment.Left;
        PropertiesPanel.VerticalAlignment = VerticalAlignment.Bottom;
        PropertiesPanel.Margin = new Thickness(20, 0, 0, 20);
    }

    private void DockBottomRight_Click(object sender, RoutedEventArgs e)
    {
        PropertiesPanel.HorizontalAlignment = HorizontalAlignment.Right;
        PropertiesPanel.VerticalAlignment = VerticalAlignment.Bottom;
        PropertiesPanel.Margin = new Thickness(0, 0, 20, 20);
    }

    public void SetViewModel(SnapScreen snapScreen, Layout layout)
    {
        var model = ViewModel; //DataContext as DesignWindowViewModel;
        if (model != null)
        {
            model.Window = this;
            model.SnapScreen = snapScreen;
            model.Layout = layout;

            if (layout.Size.Width <= 0 || layout.Size.Height <= 0)
            {
                layout.Size = new SnapIt.Common.Graphics.Size(
                    (float)snapScreen.DesignSize.Width,
                    (float)snapScreen.DesignSize.Height);
            }

            designerSizeText.Text = $"Designer Size     {snapScreen.DesignSize.Width:0.00} x {snapScreen.DesignSize.Height:0.00}";
        }

        if (Dev.IsTopmostDisabled)
        {
            Topmost = false;
        }
    }
}