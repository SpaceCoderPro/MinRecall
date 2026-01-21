using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinRecall.UI.ViewModels;
using System;

namespace MinRecall.UI.Views;

public partial class HeatmapView : UserControl
{
    public HeatmapView()
    {
        InitializeComponent();
        
        DataContext = new HeatmapViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}