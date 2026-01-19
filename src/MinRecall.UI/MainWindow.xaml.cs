using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace MinRecall.UI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBar);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer != null && args.InvokedItemContainer.Tag != null)
        {
            var tag = args.InvokedItemContainer.Tag.ToString();
            switch (tag)
            {
                case "timeline":
                    ContentFrame.Navigate(typeof(TimelinePage));
                    break;
                case "search":
                    ContentFrame.Navigate(typeof(SearchPage));
                    break;
                case "heatmap":
                    ContentFrame.Navigate(typeof(HeatmapPage));
                    break;
                case "activity":
                    ContentFrame.Navigate(typeof(ActivityPage));
                    break;
            }
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsDialog();
        _ = dialog.ShowAsync();
    }
}
