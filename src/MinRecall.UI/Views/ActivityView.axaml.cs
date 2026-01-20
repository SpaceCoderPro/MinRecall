using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinRecall.UI.ViewModels;
using System;

namespace MinRecall.UI.Views;

public partial class ActivityView : UserControl
{
    public ActivityView()
    {
        InitializeComponent();
        
        DataContext = new ActivityViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}