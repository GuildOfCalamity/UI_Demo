using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UI_Demo.Dialogs;

public sealed partial class CloseDialog : ContentDialog
{
    public CloseDialog()
    {
        this.InitializeComponent();
        this.Opened += DialogOnOpened;
        this.GotFocus += OnGotFocus;
        this.LostFocus += OnLostFocus;
    }

    /// <summary>
    ///   The "parent" of the ContentDialog is the XamlRoot of the main 
    ///   window, so we'll key off of the ContentDialog's title panel.
    /// </summary>
    void OnGotFocus(object sender, RoutedEventArgs e) => BloomHelper.AddBloom((UIElement)imgLevel, (UIElement)cdGrid, Windows.UI.Color.FromArgb(230, 250, 244, 32), 14);
    void OnLostFocus(object sender, RoutedEventArgs e) => BloomHelper.RemoveBloom((UIElement)imgLevel, (UIElement)cdGrid, null);

    void DialogOnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        if (App.Current.Resources.TryGetValue("DialogAcrylicBrush", out object _))
        {
            this.Background = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["DialogAcrylicBrush"];
        }
        else
        {
            this.Background = new AcrylicBrush
            {
                TintOpacity = 0.2,
                TintLuminosityOpacity = 0.1,
                TintColor = Windows.UI.Color.FromArgb(255, 49, 122, 215),
                FallbackColor = Windows.UI.Color.FromArgb(255, 49, 122, 215)
            };
        }
    }

}
