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
            this.GotFocus += OnGotFocus;
            this.LostFocus += OnLostFocus;
        }

        /// <summary>
        ///   The "parent" of the ContentDialog is the XamlRoot of the main 
        ///   window, so we'll key off of the ContentDialog's title panel.
        /// </summary>
        void OnGotFocus(object sender, RoutedEventArgs e)
        {
            BloomHelper.AddBloom((UIElement)imgLevel, (UIElement)cdGrid, Windows.UI.Color.FromArgb(230, 11, 203, 239), 12);
            BloomHelper.AddBloom((UIElement)tbTitle, (UIElement)cdStack, Windows.UI.Color.FromArgb(255, 255, 255, 255), 8);
        }

        void OnLostFocus(object sender, RoutedEventArgs e)
        {
            BloomHelper.RemoveBloom((UIElement)imgLevel, (UIElement)cdGrid, null);
            BloomHelper.RemoveBloom((UIElement)tbTitle, (UIElement)cdStack, null);
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
