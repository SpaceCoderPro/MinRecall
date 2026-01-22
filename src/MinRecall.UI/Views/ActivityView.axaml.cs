using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinRecall.UI.Views;

public partial class ActivityView : UserControl
{
    public ActivityView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}