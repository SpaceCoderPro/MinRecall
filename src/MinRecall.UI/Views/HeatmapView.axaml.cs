using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinRecall.UI.Views;

public partial class HeatmapView : UserControl
{
    public HeatmapView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}