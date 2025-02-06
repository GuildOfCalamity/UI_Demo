using System;
using System.IO;
using Microsoft.UI.Xaml.Data;

namespace UI_Demo
{
    public partial class FullPathToFileConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string result = string.Empty;

            //System.Diagnostics.Debug.WriteLine($"[INFO] Shortening path '{value}'");
            if (!string.IsNullOrEmpty((string)value))
            {
                result = Path.GetFileNameWithoutExtension((string)value);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
