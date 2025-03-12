using System;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;

namespace UI_Demo;

public struct PdfPageModel
{
    public int PageNumber { get; set; }

    public BitmapImage PageBitmapImage { get; set; }

    public SoftwareBitmap PageSoftwareBitmap { get; set; }
}
