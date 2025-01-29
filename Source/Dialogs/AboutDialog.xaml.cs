using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace UI_Demo.Dialogs
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();
            this.Loaded += AboutDialogOnLoaded;
            this.Unloaded += AboutDialogOnUnloaded;
        }

        void AboutDialogOnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!App.IsClosing)
                StoryboardSpin?.Stop();
        }

        void AboutDialogOnLoaded(object sender, RoutedEventArgs e)
        {
            if (!App.IsClosing)
            {
                StoryboardSpin?.Begin();
                tbRefs.Text = App.GetRuntimeSDK();
            }
        }
    }
}
