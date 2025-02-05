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
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UI_Demo.Dialogs;

public sealed partial class ResultsDialog : ContentDialog
{
    public ResultsDialog()
    {
        this.InitializeComponent();
        this.Opened += DialogOnOpened;
    }

    public ResultsDialog(string? title, string? message, MessageLevel level) : this()
    {
        if (!string.IsNullOrEmpty(title))
            tbTitle.Text = title;
        
        if (!string.IsNullOrEmpty(message))
            tbMessage.Text = message;

        switch (level)
        {
            case MessageLevel.Error:       imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation1.png")); break;
            case MessageLevel.Warning:     imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation2.png")); break;
            case MessageLevel.Information: imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation4.png")); break;
            case MessageLevel.Important:   imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation3.png")); break;
            case MessageLevel.Debug:       imgIcon.Source = new BitmapImage(new Uri("ms-appx:///Assets/Exclamation5.png")); break;
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

}
