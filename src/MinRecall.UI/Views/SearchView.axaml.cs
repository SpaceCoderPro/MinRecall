using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinRecall.UI.ViewModels;

namespace MinRecall.UI.Views;

public partial class SearchView : UserControl
{
    public SearchView()
    {
        InitializeComponent();
        
        DataContext = new SearchViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}