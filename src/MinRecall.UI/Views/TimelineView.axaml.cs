using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinRecall.UI.ViewModels;

namespace MinRecall.UI.Views;

public partial class TimelineView : UserControl
{
    public TimelineView()
    {
        InitializeComponent();
        
        DataContext = new TimelineViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}