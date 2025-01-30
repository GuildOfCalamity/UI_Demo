using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace UI_Demo.Dialogs
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();
            this.Loaded += AboutDialogOnLoaded;
            this.Unloaded += AboutDialogOnUnloaded;
            this.Opened += DialogOnOpened;
        }

        void AboutDialogOnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!App.IsClosing)
                StoryboardSpin?.Stop();
        }

        void AboutDialogOnLoaded(object sender, RoutedEventArgs e)
        {
            if (!App.IsClosing)
                StoryboardSpin?.Begin();
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
}
