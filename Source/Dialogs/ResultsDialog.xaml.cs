using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UI_Demo.Dialogs;

public sealed partial class ResultsDialog : ContentDialog
{
    static Windows.UI.Color bloomColor = Windows.UI.Color.FromArgb(230, 250, 250, 250);
    
    public ResultsDialog()
    {
        this.InitializeComponent();
        this.Opened += DialogOnOpened;
        this.GotFocus += OnGotFocus;
        this.LostFocus += OnLostFocus;
    }

    public ResultsDialog(string? title, string? message, MessageLevel level) : this()
    {
        if (!string.IsNullOrEmpty(title))
            tbTitle.Text = title;
        
        if (!string.IsNullOrEmpty(message))
            tbMessage.Text = message;


        switch (level)
        {
            case MessageLevel.Error:       
                imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation1.png"));
                bloomColor = Windows.UI.Color.FromArgb(230, 250, 123, 43);  // red
                break;
            case MessageLevel.Warning:     
                imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation2.png"));
                bloomColor = Windows.UI.Color.FromArgb(230, 250, 188, 35); // orange
                break;
            case MessageLevel.Important:   
                imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation3.png"));
                bloomColor = Windows.UI.Color.FromArgb(230, 250, 244, 32); // yellow
                break;
            case MessageLevel.Information: 
                imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation4.png"));
                bloomColor = Windows.UI.Color.FromArgb(230, 11, 203, 239); // blue
                break;
            case MessageLevel.Debug:       
                imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation5.png"));
                bloomColor = Windows.UI.Color.FromArgb(230, 30, 250, 84);  // green
                break;
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
    void OnGotFocus(object sender, RoutedEventArgs e) => BloomHelper.AddBloom((UIElement)imgIcon, (UIElement)cdGrid, bloomColor, 14);
    void OnLostFocus(object sender, RoutedEventArgs e) => BloomHelper.RemoveBloom((UIElement)imgIcon, (UIElement)cdGrid, null);
}
