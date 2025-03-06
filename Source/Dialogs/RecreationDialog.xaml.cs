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

/// <summary>
/// This dialog is for testing assets.
/// </summary>
public sealed partial class RecreationDialog : ContentDialog
{
    public RecreationDialog()
    {
        this.InitializeComponent();
        this.Loaded += RecreationDialogOnLoaded;
        this.Unloaded += RecreationDialogOnUnloaded;
        this.Opened += DialogOnOpened;
        this.GotFocus += OnGotFocus;
        this.LostFocus += OnLostFocus;
    }

    void RecreationDialogOnLoaded(object sender, RoutedEventArgs e)
    {
        if (!App.IsClosing)
        {
            StoryboardSpin1?.Begin();
            StoryboardSpin2?.Begin();
            StoryboardSpin3?.Begin();
            StoryboardSpin4?.Begin();
            StoryboardSpin5?.Begin();
            StoryboardSpin6?.Begin();
        }
    }

    void RecreationDialogOnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!App.IsClosing)
        {
            StoryboardSpin1?.Stop();
            StoryboardSpin2?.Stop();
            StoryboardSpin3?.Stop();
            StoryboardSpin4?.Stop();
            StoryboardSpin5?.Stop();
            StoryboardSpin6?.Stop();
        }
    }

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

    /// <summary>
    ///   The "parent" of the ContentDialog is the XamlRoot of the main 
    ///   window, so we'll key off of the ContentDialog's title panel.
    /// </summary>
    void OnGotFocus(object sender, RoutedEventArgs e)
    {
        BloomHelper.AddBloom((UIElement)imgIcon, (UIElement)cdGrid, Windows.UI.Color.FromArgb(180, 255, 255, 255), 11);
        BloomHelper.AddBloom((UIElement)imgIcon, (UIElement)cdGrid, Windows.UI.Color.FromArgb(220, 78, 66, 173), 22);
    }
    void OnLostFocus(object sender, RoutedEventArgs e) => BloomHelper.RemoveBloom((UIElement)imgIcon, (UIElement)cdGrid, null);
}
