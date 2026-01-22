using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinRecall.UI.Views;

public partial class TimelineView : UserControl
{
    public TimelineView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}