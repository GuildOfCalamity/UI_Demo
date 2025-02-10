using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace UI_Demo;

/// <summary>
///   Provides the practical object source type for the Image.Source and ImageBrush.ImageSource properties. 
///   You can define a BitmapImage by using a Uniform Resource Identifier (URI) that references an image 
///   source file, or by calling SetSourceAsync and supplying a stream.
///   https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.bitmapimage?view=winrt-22621
///   A BitmapImage can be sourced from these image file formats:
///   - Joint Photographic Experts Group (JPEG)
///   - Portable Network Graphics (PNG)
///   - Bitmap (BMP)
///   - Graphics Interchange Format (GIF)
///   - Tagged Image File Format (TIFF)
///   - JPEG XR
///   - Icon (ICO)
/// </summary>
/// <remarks>
///   If the image source is a stream, that stream is expected to contain an image file in one of these formats.
///   The BitmapImage class represents an abstraction so that an image source can be set asynchronously but still 
///   be referenced in XAML markup as a property value, or in code as an object that doesn't use awaitable syntax. 
///   When you create a BitmapImage object in code, it initially has no valid source. You should then set its source 
///   using one of these techniques:
///   Use the BitmapImage(Uri) constructor rather than the default constructor. Although it's a constructor you can 
///   think of this as having an implicit asynchronous behavior: the BitmapImage won't be ready for use until it 
///   raises an ImageOpened event that indicates a successful async source set operation.
///   Set the UriSource property. As with using the Uri constructor, this action is implicitly asynchronous, and the 
///   BitmapImage won't be ready for use until it raises an ImageOpened event.
///   Use SetSourceAsync. This method is explicitly asynchronous. The properties where you might use a BitmapImage, 
///   such as Image.Source, are designed for this asynchronous behavior, and won't throw exceptions if they are set 
///   using a BitmapImage that doesn't have a complete source yet. Rather than handling exceptions, you should handle 
///   ImageOpened or ImageFailed events either on the BitmapImage directly or on the control that uses the source 
///   (if those events are available on the control class).
///   ImageFailed and ImageOpened are mutually exclusive. One event or the other will always be raised whenever a 
///   BitmapImage object has its source value set or reset.
///   The API for Image, BitmapImage and BitmapSource doesn't include any dedicated methods for encoding and decoding 
///   of media formats. All of the encode and decode operations are built-in, and at most will surface aspects of 
///   encode or decode as part of event data for load events. 
///   If you want to do any special work with image encode or decode, which you might use if your app is doing image 
///   conversions or manipulation, you should use the API that are available in the Windows.Graphics.Imaging namespace.
/// </remarks>
public static class AppCapture
{
    static int _counter = 0; // for file naming

    /*  --[frame-based screenshot capture example]--
    
        DispatcherTimer? tmr;

        // Create a timer for capturing animations during page load.
        tmr = new DispatcherTimer();
        tmr.Interval = TimeSpan.FromMilliseconds(100);
        tmr.Tick += tmrOnTick;
    
        async void tmrOnTick(object? sender, object e) => await UpdateScreenshot(rootGrid, null);
    */

    #region [Capture Routines]
    public static async Task<Windows.Graphics.Imaging.SoftwareBitmap> GetScreenshot(Microsoft.UI.Xaml.UIElement root)
    {
        Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new();

        // Try and use the root's native size first, if that fails then use the app's static size.
        var ras = root.ActualSize.ToSize();
        if (ras.Width != double.NaN && ras.Height != double.NaN && ras.Width > 0 && ras.Height > 0)
            await renderTargetBitmap.RenderAsync(root, (int)(ras.Width + 1), (int)(ras.Height + 1));
        else
            await renderTargetBitmap.RenderAsync(root, App.m_width, App.m_height);

        // Convert RenderTargetBitmap to SoftwareBitmap
        IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        byte[] pixels = pixelBuffer.ToArray();
        if (pixels.Length == 0 || renderTargetBitmap.PixelWidth == 0 || renderTargetBitmap.PixelHeight == 0)
        {
            Debug.WriteLine($"[WARNING] The width and height are not valid.");
        }

        Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
        softwareBitmap.CopyFromBuffer(pixelBuffer);
        return softwareBitmap;
    }

    /// <summary>
    ///   I'm using this as a sort of "poor man's screen capture".
    ///   Apply a page's visual state to an <see cref="Microsoft.UI.Xaml.Controls.Image"/> source. 
    ///   If the target is null then the image is saved to disk.
    /// </summary>
    /// <param name="root">host <see cref="Microsoft.UI.Xaml.UIElement"/>. Can be a grid, a page, etc.</param>
    /// <param name="target">optional <see cref="Microsoft.UI.Xaml.Controls.Image"/> target</param>
    /// <remarks>
    ///   Using a RenderTargetBitmap, you can accomplish scenarios such as applying image effects to a visual that 
    ///   originally came from a XAML UI composition, generating thumbnail images of child pages for a navigation 
    ///   system, or enabling the user to save parts of the UI as an image source and then share that image with 
    ///   other applications. 
    ///   Because RenderTargetBitmap is a subclass of <see cref="Microsoft.UI.Xaml.Media.ImageSource"/>, 
    ///   it can be used as the image source for <see cref="Microsoft.UI.Xaml.Controls.Image"/> elements or 
    ///   an <see cref="Microsoft.UI.Xaml.Media.ImageBrush"/> brush. 
    ///   Calling RenderAsync() provides a useful image source but the full buffer representation of 
    ///   rendering content is not copied out of video memory until the app calls GetPixelsAsync().
    ///   It is faster to call RenderAsync() only, without calling GetPixelsAsync, and use the RenderTargetBitmap as 
    ///   an <see cref="Microsoft.UI.Xaml.Controls.Image"/> or <see cref="Microsoft.UI.Xaml.Media.ImageBrush"/> 
    ///   source if the app only intends to display the rendered content and does not need the pixel data. 
    ///   [Stipulations]
    ///    - Content that's in the tree but with its Visibility set to Collapsed won't be captured.
    ///    - Content that's not directly connected to the XAML visual tree and the content of the main window won't be captured. This includes Popup content, which is considered to be like a sub-window.
    ///    - Content that can't be captured will appear as blank in the captured image, but other content in the same visual tree can still be captured and will render (the presence of content that can't be captured won't invalidate the entire capture of that XAML composition).
    ///    - Content that's in the XAML visual tree but offscreen can be captured, so long as it's not Visibility = Collapsed.
    ///   https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.rendertargetbitmap?view=winrt-22621
    /// </remarks>
    public static async Task UpdateScreenshot(Microsoft.UI.Xaml.UIElement root, Microsoft.UI.Xaml.Controls.Image? target)
    {
        Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new();

        // Try and use the root's native size first, if that fails then use the app's static size.
        var ras = root.ActualSize.ToSize();
        if (ras.Width != double.NaN && ras.Height != double.NaN && ras.Width > 0 && ras.Height > 0)
            await renderTargetBitmap.RenderAsync(root, (int)(ras.Width + 1), (int)(ras.Height + 1));
        else
            await renderTargetBitmap.RenderAsync(root, App.m_width, App.m_height);

        // If you want the image to be applied to the UIElement, pass in a target control of type Image.
        if (target is not null)
        {
            // A render target bitmap is a viable ImageSource.
            target.Source = renderTargetBitmap;
        }
        else // save to disk
        {
            // Convert RenderTargetBitmap to SoftwareBitmap
            IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();
            if (pixels.Length == 0 || renderTargetBitmap.PixelWidth == 0 || renderTargetBitmap.PixelHeight == 0)
            {
                Debug.WriteLine($"[ERROR] The width and height are not valid, cannot save.");
            }
            else
            {
                Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pixelBuffer);
                _counter++;
                Debug.WriteLine($"[INFO] Saving frame #{_counter} to disk.");
                await SaveSoftwareBitmapToFileAsync(softwareBitmap, Path.Combine(AppContext.BaseDirectory, $"Frame_{_counter.ToString().PadLeft(3, '0')}.png"), Windows.Graphics.Imaging.BitmapInterpolationMode.NearestNeighbor);
                softwareBitmap.Dispose();
            }
        }
    }

    /// <summary>
    ///   Uses a <see cref="Windows.Graphics.Imaging.BitmapEncoder"/> to save the output.
    /// </summary>
    /// <param name="softwareBitmap"><see cref="Windows.Graphics.Imaging.SoftwareBitmap"/></param>
    /// <param name="filePath">output file path to save</param>
    /// <param name="interpolation">In general, moving from NearestNeighbor to Fant, interpolation quality increases while performance decreases.</param>
    /// <remarks>
    ///   Assumes <see cref="Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId"/>.
    ///   [Windows.Graphics.Imaging.BitmapInterpolationMode]
    ///   3 Fant...........: A Fant resampling algorithm. Destination pixel values are computed as a weighted average of the all the pixels that map to the new pixel in a box shaped kernel.
    ///   2 Cubic..........: A bicubic interpolation algorithm. Destination pixel values are computed as a weighted average of the nearest sixteen pixels in a 4x4 grid.
    ///   1 Linear.........: A bilinear interpolation algorithm. The output pixel values are computed as a weighted average of the nearest four pixels in a 2x2 grid.
    ///   0 NearestNeighbor: A nearest neighbor interpolation algorithm. Also known as nearest pixel or point interpolation. The output pixel is assigned the value of the pixel that the point falls within. No other pixels are considered.
    /// </remarks>
    public static async Task SaveSoftwareBitmapToFileAsync(Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap, string filePath, Windows.Graphics.Imaging.BitmapInterpolationMode interpolation = Windows.Graphics.Imaging.BitmapInterpolationMode.Fant)
    {
        if (File.Exists(filePath))
        {
            Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                Windows.Graphics.Imaging.BitmapEncoder encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                encoder.BitmapTransform.InterpolationMode = interpolation;
                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] SaveSoftwareBitmapToFileAsync({ex.HResult}): {ex.Message}");
                }
            }
        }
        else
        {
            // Get the folder and file name from the file path.
            string? folderPath = System.IO.Path.GetDirectoryName(filePath);
            string? fileName = System.IO.Path.GetFileName(filePath);
            // Create the folder if it does not exist.
            Windows.Storage.StorageFolder storageFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            {
                Windows.Graphics.Imaging.BitmapEncoder encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(softwareBitmap);
                encoder.BitmapTransform.InterpolationMode = interpolation;
                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] SaveSoftwareBitmapToFileAsync({ex.HResult}): {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    ///   This was not trivial and proved to be a challenge when UriSource is unavailable.
    ///   Because we're extracting the asset from a DLL, the UriSource is null which immediately limits our options.
    ///   I'm sure someone will correct my misadventure, but this works — and you can't argue with results.
    /// </summary>
    /// <param name="hostGrid"><see cref="Microsoft.UI.Xaml.Controls.Grid"/> to serve as the liaison.</param>
    /// <param name="imageSource"><see cref="Microsoft.UI.Xaml.Media.ImageSource"/> to save.</param>
    /// <param name="filePath">The full path to write the image.</param>
    /// <param name="width">16 to 256</param>
    /// <param name="height">16 to 256</param>
    /// <remarks>
    ///   If the width or height is not correct the render target cannot be saved.
    ///   The following types derive from <see cref="Microsoft.UI.Xaml.Media.ImageSource"/>:
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapSource"/>
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"/>
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.SoftwareBitmapSource"/>
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.SurfaceImageSource"/>
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.SvgImageSource"/>
    /// </remarks>
    public static async Task SaveImageSourceToFileAsync(Microsoft.UI.Xaml.Controls.Grid hostGrid, Microsoft.UI.Xaml.Media.ImageSource imageSource, string filePath, int width = 800, int height = 600)
    {
        // Create an Image control to hold the ImageSource
        Microsoft.UI.Xaml.Controls.Image imageControl = new Microsoft.UI.Xaml.Controls.Image
        {
            Source = imageSource,
            Width = width,
            Height = height,
            Opacity = 0.01
        };

        // This is clunky, but for some reason the Image resource is never
        // fully created if it's not appended to a rendered host control; as a
        // workaround we'll add the Image control to the host Grid and remove later.
        hostGrid.Children.Add(imageControl);

        // Wait for the image to be loaded and rendered (don't go below 20ms).
        await Task.Delay(20);

        // Render the Image control to a RenderTargetBitmap
        Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new();
        await renderTargetBitmap.RenderAsync(imageControl);

        // Convert RenderTargetBitmap to SoftwareBitmap
        IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        byte[] pixels = pixelBuffer.ToArray();

        // Remove the Image control from the host Grid
        hostGrid.Children.Remove(imageControl);

        try
        {
            if (pixels.Length == 0)
            {
                Debug.WriteLine($"[ERROR] No pixel buffer data to write. Try adjusting the delay time.");
            }
            else
            {
                Debug.WriteLine($"[INFO] Attempting to write {pixels.Length} bytes of pixel data.");
                // NOTE: A SoftwareBitmap displayed in a XAML app must be in BGRA pixel format with pre-multiplied alpha values.
                Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(
                    Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                    renderTargetBitmap.PixelWidth,
                    renderTargetBitmap.PixelHeight,
                    Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pixelBuffer);
                // Save SoftwareBitmap to file
                await SaveSoftwareBitmapToFileAsync(softwareBitmap, filePath, Windows.Graphics.Imaging.BitmapInterpolationMode.NearestNeighbor);
                softwareBitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] SaveImageSourceToFileAsync: {ex.Message}");
        }
    }

    /// <summary>
    ///   This was not trivial and proved to be a challenge when UriSource is unavailable.
    ///   Because we're extracting the asset from a DLL, the UriSource is null which immediately limits our options.
    ///   I'm sure someone will correct my misadventure, but this works — and you can't argue with results.
    /// </summary>
    /// <param name="hostGrid"><see cref="Microsoft.UI.Xaml.Controls.Grid"/> to serve as the liaison.</param>
    /// <param name="imageSource"><see cref="Microsoft.UI.Xaml.Media.ImageSource"/> to save.</param>
    /// <param name="filePath">The full path to write the image.</param>
    /// <param name="width">16 to 256</param>
    /// <param name="height">16 to 256</param>
    /// <remarks>
    ///   If the width or height is not correct the render target cannot be saved.
    ///   The following types derive from <see cref="Microsoft.UI.Xaml.Media.ImageSource"/>:
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapSource"/>
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"/>
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.SoftwareBitmapSource"/>
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.SurfaceImageSource"/>
    ///    - <see cref="Microsoft.UI.Xaml.Media.Imaging.SvgImageSource"/>
    /// </remarks>
    public static async Task SaveImageSourceToFileAsync(Microsoft.UI.Xaml.Controls.Grid hostGrid, Microsoft.UI.Xaml.Controls.Image image, BitmapImage bmpImg, string filePath, int width = 800, int height = 600)
    {

        if (image == null)
            throw new ArgumentNullException(nameof(image));
        if (bmpImg == null)
            throw new ArgumentNullException(nameof(bmpImg));


        // Create an Image control to hold the ImageSource.
        // Luckily the opacity is not preserved when the image is saved, so we
        // can set it very low to prevent the disturbing pop-in effect to the user.
        Microsoft.UI.Xaml.Controls.Image imageControl = new Microsoft.UI.Xaml.Controls.Image
        {
            Source = bmpImg,
            Width = width,
            Height = height,
            Opacity = 0.01
        };

        try
        {   
            image.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

            // This is clunky, but for some reason the Image resource is never
            // fully created if it's not appended to a rendered host control; as a
            // workaround we'll add the Image control to the host Grid and remove later.
            hostGrid.Children.Add(imageControl);

            // Wait for the image to be loaded and rendered (don't go below 20ms).
            // There has to be some delay here whilst the dispatcher does its magic.
            await Task.Delay(20);

            // Render the Image control to a RenderTargetBitmap
            Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap = new();
            await renderTargetBitmap.RenderAsync(imageControl);

            // Convert RenderTargetBitmap to SoftwareBitmap
            IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();

            image.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

            // Remove the Image control from the host Grid
            hostGrid.Children.Remove(imageControl);

            if (pixels.Length == 0)
            {
                Debug.WriteLine($"[ERROR] No pixel buffer data to write. Try adjusting the delay time.");
            }
            else
            {
                Debug.WriteLine($"[INFO] Attempting to write {pixels.Length} bytes of pixel data.");
                // NOTE: A SoftwareBitmap displayed in a XAML app must be in BGRA pixel format with pre-multiplied alpha values.
                Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(
                    Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                    renderTargetBitmap.PixelWidth,
                    renderTargetBitmap.PixelHeight,
                    Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pixelBuffer);
                // Save SoftwareBitmap to file
                await SaveSoftwareBitmapToFileAsync(softwareBitmap, filePath, Windows.Graphics.Imaging.BitmapInterpolationMode.NearestNeighbor);
                softwareBitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] SaveImageSourceToFileAsync: {ex.Message}");
        }
    }

    /// <summary>
    ///   Saves a <see cref="BitmapImage"/> to the given <paramref name="filePath"/>.
    /// </summary>
    /// <remarks>
    ///   This does seem to update the file, but does not actually save the pixel data since each byte is zero.
    ///   It would seem that there's no way to save a <see cref="BitmapImage"/> without a valid UriSource.
    /// </remarks>
    public static async Task SaveBitmapImageToDisk(BitmapImage bitmapImage, string filePath)
    {
        SoftwareBitmap? softwareBitmap;
        StorageFile? file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
        try
        {
            if (file == null || bitmapImage == null) { return; }

            // NOTE: If a SoftwareBitmap is to be displayed in a XAML app it must be in BGRA pixel format with pre-multiplied alpha values.
            softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, bitmapImage.PixelWidth, bitmapImage.PixelHeight, BitmapAlphaMode.Premultiplied);

            #region [In one step]
            //using (var inStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
            //{
            //    var decoder = await BitmapDecoder.CreateAsync(inStream);
            //    softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            //    if (softwareBitmap != null)
            //    {
            //        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, inStream);
            //        encoder.SetSoftwareBitmap(softwareBitmap);
            //        await encoder.FlushAsync();
            //    }
            //}
            #endregion

            #region [In two steps]
            using (var inStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(inStream);
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                Debug.WriteLine($"[INFO] SoftwareBitmap is {softwareBitmap.PixelWidth}, {softwareBitmap.PixelHeight}");
            }

            if (softwareBitmap != null)
            {
                using (var outStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
                    encoder.SetSoftwareBitmap(softwareBitmap);
                    await encoder.FlushAsync();
                }
            }
            #endregion

        }
        catch (Exception ex) 
        {
            Debug.WriteLine($"[ERROR] SaveBitmapImageToDisk({ex.HResult}): {ex.Message}");
        }
    }
    #endregion

    #region [Helpers & Test Methods]
    /// <summary>
    /// Assumes PNG output via <see cref="Windows.Graphics.Imaging.BitmapEncoder"/>.
    /// </summary>
    /// <param name="bitmapImage"><see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/></param>
    /// <remarks>
    /// This assumes the <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/> contains 
    /// a UriSource, which will be used in conjunction with the ToStream() helper.
    /// </remarks>
    /// <returns><see cref="Windows.Graphics.Imaging.SoftwareBitmap"/></returns>
    public static async Task<SoftwareBitmap> GetSoftwareBitmapFromBitmapImageAsync(BitmapImage bitmapImage)
    {
        uint width = 0;
        uint height = 0;

        if (bitmapImage.UriSource == null)
            throw new Exception($"The {nameof(bitmapImage)} {nameof(bitmapImage.UriSource)} cannot be empty.");

        if (bitmapImage.PixelWidth == 0 || bitmapImage.PixelHeight == 0)
        {
            width = (uint)App.m_width;
            height = (uint)App.m_height;
        }
        else
        {
            width= (uint)bitmapImage.PixelWidth;
            height= (uint)bitmapImage.PixelHeight;
        }

        // Retrieve pixel data from the BitmapImage
        using (InMemoryRandomAccessStream stream = new())
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            Stream pixelStream = bitmapImage.ToStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);
            // NOTE: A SoftwareBitmap displayed in a XAML app must be in BGRA pixel format with pre-multiplied alpha values.
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, width, height, 96.0, 96.0, pixels);
            await encoder.FlushAsync();
            // Decode the image to a SoftwareBitmap
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            return await decoder.GetSoftwareBitmapAsync();
        }
    }

    /// <summary>
    /// Returns a <see cref="Windows.Storage.Streams.InMemoryRandomAccessStream"/> from a <see cref="Microsoft.UI.Xaml.UIElement"/>.
    /// </summary>
    /// <param name="element"><see cref="Microsoft.UI.Xaml.UIElement"/></param>
    public static async Task<RandomAccessStreamReference> GetRandomAccessStreamFromUIElement(UIElement? element)
    {
        RenderTargetBitmap renderTargetBitmap = new();
        InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
        // Render to an image at the current system scale and retrieve pixel contents
        await renderTargetBitmap.RenderAsync(element);
        var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        // Encode image to an in-memory stream.
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore,
            (uint)renderTargetBitmap.PixelWidth,
            (uint)renderTargetBitmap.PixelHeight,
            96d,
            96d,
            pixelBuffer.ToArray());
        await encoder.FlushAsync();
        // Set content to the encoded image in memory.
        return RandomAccessStreamReference.CreateFromStream(stream);
    }

    /// <summary>
    /// Returns the <see cref="UIElement"/> as an array of bytes.
    /// </summary>
    /// <param name="control"></param>
    public static async Task<byte[]> GetUIElementAsPngBytes(UIElement control)
    {
        // Get XAML Visual in BGRA8 format
        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(control, (int)control.ActualSize.X, (int)control.ActualSize.Y);

        // Encode as PNG
        var pixelBuffer = (await rtb.GetPixelsAsync()).ToArray();
        IRandomAccessStream mraStream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, mraStream);
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)rtb.PixelWidth,
            (uint)rtb.PixelHeight,
            96,
            96,
            pixelBuffer);
        await encoder.FlushAsync();

        // Transform to byte array
        var bytes = new byte[mraStream.Size];
        await mraStream.ReadAsync(bytes.AsBuffer(), (uint)mraStream.Size, InputStreamOptions.None);

        return bytes;
    }

    /// <summary>
    /// Convert <see cref="byte[]"/> to <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/>
    /// via <see cref="Windows.Storage.Streams.DataWriter"/>.
    /// </summary>
    /// <param name="data"><see cref="byte[]"/></param>
    /// <returns><see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/> if successful, null otherwise</returns>
    public static async Task<Microsoft.UI.Xaml.Media.Imaging.BitmapImage?> GetBitmapImageFromBytesAsync(byte[] data)
    {
        try
        {
            var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
            {
                using (var writer = new Windows.Storage.Streams.DataWriter(stream))
                {
                    writer.WriteBytes(data);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream(); // probably called after leaving using scope
                }
                stream.Seek(0);
                await bitmapImage.SetSourceAsync(stream);
            }
            return bitmapImage;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetBitmapAsync: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///  Set the <see cref="Microsoft.UI.Xaml.Controls.Image"/> source from a URL.
    /// </summary>
    /// <param name="imageUrl">the URL of the image</param>
    /// <param name="imageControl"></param>
    public static async void SetImageSourceFromUrl(string imageUrl, Image imageControl)
    {
        using (System.Net.Http.HttpClient client = new())
        {
            Stream? imgStream = await client.GetStreamAsync(imageUrl);

            // Create SoftwareBitmap from the stream.
            MemoryStream ms = new MemoryStream();
            await imgStream.CopyToAsync(ms);
            var decoder = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());
            var softBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            // Use SetBitmapAsync to the Image's source.
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softBitmap);
            imageControl.DispatcherQueue.TryEnqueue(() => imageControl.Source = source);
        }
    }

    /// <summary>
    ///   Encodes a SoftwareBitmap and writes it to a stream.
    /// </summary>
    /// <remarks>
    ///   Assumes PNG format.
    /// </remarks>
    public static async Task EncodeAndSaveSoftwareBitmapAsync(SoftwareBitmap softwareBitmap, IRandomAccessStream stream)
    {
        try
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] EncodeAndSaveSoftwareBitmapAsync({ex.HResult}): {ex.Message}");
        }
    }

    /// <summary>
    ///   Converts RenderTargetBitmap to SoftwareBitmap.
    /// </summary>
    public static async Task<SoftwareBitmap> ConvertRenderTargetBitmapToSoftwareBitmapAsync(RenderTargetBitmap renderTargetBitmap)
    {
        var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(
            pixelBuffer,
            BitmapPixelFormat.Bgra8,
            App.m_width, App.m_height,
            BitmapAlphaMode.Premultiplied);
        return softwareBitmap;
    }

    /// <summary>
    ///  Uses a <see cref="BitmapEncoder"/> to flush pixel data into the <see cref="IRandomAccessStream"/>.
    /// </summary>
    /// <param name="renderTargetBitmap"><see cref="RenderTargetBitmap"/></param>
    /// <param name="stream"><see cref="IRandomAccessStream"/></param>
    public static async Task EncodeAndSaveRenderTargetBitmapAsync(RenderTargetBitmap renderTargetBitmap, IRandomAccessStream stream)
    {
        var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                             BitmapAlphaMode.Premultiplied,
                             (uint)App.m_width,
                             (uint)App.m_height,
                             96, 96,
                             pixelBuffer.ToArray());
        await encoder.FlushAsync();
    }

    /// <summary>
    /// Saves a BitmapImage to a file (PNG format) via an <see cref="Microsoft.UI.Xaml.Controls.Image"/>.
    /// </summary>
    /// <remarks>
    /// The <paramref name="imageControl"/> must be visible when calling this method.
    /// </remarks>
    public static async Task SaveBitmapImageToFileAsync(BitmapImage bitmapImage, Microsoft.UI.Xaml.Controls.Image imageControl, RenderTargetBitmap renderTargetBitmap, string filePath)
    {
        if (bitmapImage == null)
            throw new ArgumentNullException(nameof(bitmapImage));
        if (imageControl == null)
            throw new ArgumentNullException(nameof(imageControl));
        if (renderTargetBitmap == null)
            throw new ArgumentNullException(nameof(renderTargetBitmap));

        // Render BitmapImage to a RenderTargetBitmap
        imageControl = new Microsoft.UI.Xaml.Controls.Image
        {
            Source = bitmapImage,
            Width = App.m_width,
            Height = App.m_height
        };

        try
        {   // Render the Image control to a RenderTargetBitmap
            await renderTargetBitmap.RenderAsync(imageControl); // **Image must be in UI**

            // Wait for the image to be loaded (down to 20ms seems to work).
            await Task.Delay(40);

            // Convert RenderTargetBitmap to SoftwareBitmap
            IBuffer pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            byte[] pixels = pixelBuffer.ToArray();

            if (pixels.Length == 0 || renderTargetBitmap.PixelWidth == 0 || renderTargetBitmap.PixelHeight == 0)
            {
                Debug.WriteLine($"[ERROR] The width and height are not a match for this asset. Try a different value other than {App.m_width},{App.m_height}.");
            }
            else
            {
                // NOTE: A SoftwareBitmap displayed in a XAML app must be in BGRA pixel format with pre-multiplied alpha values.
                Windows.Graphics.Imaging.SoftwareBitmap softwareBitmap = new Windows.Graphics.Imaging.SoftwareBitmap(
                    Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                    renderTargetBitmap.PixelWidth,
                    renderTargetBitmap.PixelHeight,
                    Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pixelBuffer);
                // Save SoftwareBitmap to file
                await SaveSoftwareBitmapToFileAsync(softwareBitmap, filePath, Windows.Graphics.Imaging.BitmapInterpolationMode.NearestNeighbor);
                softwareBitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] SaveBitmapImageToFileAsync: {ex.Message}");
        }
    }

    /// <summary>
    ///   Converts a <see cref="BitmapImage"/> to a <see cref="SoftwareBitmap"/>.
    /// </summary>
    /// <remarks>
    ///   The <see cref="BitmapImage"/> must contain a UriSource.
    /// </remarks>
    public static async Task<SoftwareBitmap?> ConvertBitmapImageToSoftwareBitmapIfValidUriSourceAsync(BitmapImage bitmapImage)
    {
        Uri uri = bitmapImage.UriSource;
        if (uri == null)
            return null; // BitmapImage is not loaded from a URI

        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            return await decoder.GetSoftwareBitmapAsync();
        }
    }

    public static async Task<BitmapImage?> ToBitmapAsync(this byte[]? data, int decodeSize = -1)
    {
        if (data is null)
            return null;

        try
        {
            using var ms = new MemoryStream(data);
            var image = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            if (decodeSize > 0)
            {
                image.DecodePixelWidth = decodeSize;
                image.DecodePixelHeight = decodeSize;
            }
            image.DecodePixelType = Microsoft.UI.Xaml.Media.Imaging.DecodePixelType.Logical;
            await image.SetSourceAsync(ms.AsRandomAccessStream());
            return image;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ToBitmapAsync: {ex.Message}");
            return null;
        }
    }

    public static Stream ToStream(this Microsoft.UI.Xaml.Media.ImageSource imageSource)
    {
        if (imageSource is null)
            throw new ArgumentNullException($"'{nameof(imageSource)}' cannot be null");

        switch (imageSource)
        {
            case BitmapImage bitmapImage:
                {
                    if (bitmapImage.UriSource is null)
                        throw new ArgumentNullException($"'{nameof(bitmapImage.UriSource)}' cannot be null");

                    var uri = bitmapImage.UriSource;
                    return uri.ToStream();
                }
            default:
                throw new NotImplementedException($"ImageSource type: {imageSource?.GetType()} is not supported");
        }
    }

    public static async Task<Stream> ToStreamAsync(this Microsoft.UI.Xaml.Media.ImageSource imageSource, CancellationToken cancellationToken = default)
    {
        if (imageSource is null)
            throw new ArgumentNullException($"'{nameof(imageSource)}' cannot be null");

        switch (imageSource)
        {
            case Microsoft.UI.Xaml.Media.Imaging.BitmapImage bitmapImage:
                {
                    if (bitmapImage.UriSource is null)
                        throw new ArgumentNullException($"'{nameof(bitmapImage.UriSource)}' cannot be null");

                    var uri = bitmapImage.UriSource;
                    return await uri.ToStreamAsync(cancellationToken).ConfigureAwait(true);
                }

            default:
                throw new NotImplementedException($"ImageSource type: {imageSource?.GetType()} is not supported");
        }
    }

    /// <summary>
    /// Returns an encoder <see cref="Guid"/> based on the <paramref name="fileName"/> extension.
    /// </summary>
    public static Guid GetEncoderId(string fileName)
    {
        var ext = Path.GetExtension(fileName);

        if (new[] { ".bmp", ".dib" }.Contains(ext))
            return Windows.Graphics.Imaging.BitmapEncoder.BmpEncoderId;
        else if (new[] { ".tiff", ".tif" }.Contains(ext))
            return Windows.Graphics.Imaging.BitmapEncoder.TiffEncoderId;
        else if (new[] { ".gif" }.Contains(ext))
            return Windows.Graphics.Imaging.BitmapEncoder.GifEncoderId;
        else if (new[] { ".jpg", ".jpeg", ".jpe", ".jfif", ".jif" }.Contains(ext))
            return Windows.Graphics.Imaging.BitmapEncoder.JpegEncoderId;
        else if (new[] { ".hdp", ".jxr", ".wdp" }.Contains(ext))
            return Windows.Graphics.Imaging.BitmapEncoder.JpegXREncoderId;
        else if (new[] { ".heic", ".heif", ".heifs" }.Contains(ext))
            return Windows.Graphics.Imaging.BitmapEncoder.HeifEncoderId;
        else // default will be PNG
            return Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId;
    }
    #endregion

    #region [Utility Methods]
    internal static Stream ToStream(this Uri uri)
    {
        if (uri is null)
            throw new ArgumentNullException($"'{nameof(uri)}' cannot be null");

        var prefix = uri.Scheme switch
        {
            "ms-appx" or "ms-appx-web" => AppContext.BaseDirectory,
            _ => string.Empty,
        };
        // additional schemes, like ms-appdata could be added here
        // see: https://learn.microsoft.com/en-us/windows/uwp/app-resources/uri-schemes
        var absolutePath = $"{prefix}{uri.LocalPath}";

        return File.OpenRead(absolutePath);
    }

    internal static async Task<Stream> ToStreamAsync(this Uri uri, CancellationToken cancellationToken = default)
    {
        if (App.IsPackaged)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            return await file.OpenStreamForReadAsync().ConfigureAwait(true);
        }

        return uri.ToStream();
    }

    /// <summary>
    /// Converts an IBuffer (such as WriteableBitmap.PixelBuffer) into a byte array.
    /// </summary>
    public static byte[] ToByteArray(this IBuffer buffer)
    {
        using (Windows.Storage.Streams.DataReader reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
        {
            byte[] bytes = new byte[buffer.Length];
            reader.ReadBytes(bytes);
            return bytes;
        }
    }

    /// <summary>
    /// Extracts pixel data from a <see cref="WriteableBitmap"/>.
    /// </summary>
    public static async Task<byte[]> GetPixelDataAsync(this WriteableBitmap writeableBitmap)
    {
        using (Stream pixelStream = writeableBitmap.PixelBuffer.AsStream())
        {
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);
            return pixels;
        }
    }

    /// <summary>
    /// Extracts pixel data from a <see cref="RenderTargetBitmap"/>.
    /// </summary>
    public static async Task<byte[]> GetPixelDataAsync(this RenderTargetBitmap renderTargetBitmap)
    {
        if (renderTargetBitmap == null)
            throw new ArgumentNullException(nameof(renderTargetBitmap));

        var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
        return pixelBuffer.ToArray();
    }

    /// <summary>
    /// Reads pixel data from a byte array using <see cref="Windows.Storage.Streams.DataReader"/>.
    /// </summary>
    public static byte[] ReadPixelBufferWithDataReader(byte[] pixelData)
    {
        using (DataReader dataReader = DataReader.FromBuffer(pixelData.AsBuffer()))
        {
            byte[] pixels = new byte[pixelData.Length];
            dataReader.ReadBytes(pixels);
            return pixels;
        }
    }

    public static async Task<byte[]> GetImageFileBytesAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The image file does not exist.", filePath);

        Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);

        try
        {
            using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                // Decode the image
                Windows.Graphics.Imaging.BitmapDecoder decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);

                // Extract pixel data into a buffer (bitmap frame)
                Windows.Graphics.Imaging.PixelDataProvider pixelDataProvider = await decoder.GetPixelDataAsync();

                // Read pixel data using DataReader
                return ReadPixelBufferWithDataReader(pixelDataProvider.DetachPixelData());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] GetImageFileBytesAsync: {ex.Message}");
            return new byte[0];
        }
    }

    public static async Task<bool> WriteBytesUsingDataWriterAsync(byte[] data, string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    using (DataWriter dataWriter = new DataWriter(stream))
                    {
                        dataWriter.WriteBytes(data);
                        await dataWriter.StoreAsync();
                        await dataWriter.FlushAsync();
                    }
                }
                return true;
            }
            else
            {
                string? folderPath = System.IO.Path.GetDirectoryName(filePath);
                string? fileName = System.IO.Path.GetFileName(filePath);
                Windows.Storage.StorageFolder storageFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
                Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    using (DataWriter dataWriter = new DataWriter(stream))
                    {
                        dataWriter.WriteBytes(data);
                        await dataWriter.StoreAsync();
                        await dataWriter.FlushAsync();
                    }
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] WriteBytesUsingDataWriterAsync: {ex.Message}");
            return false;
        }
    }

    public static async Task<byte[]> ReadBytesUsingDataReaderAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
            using (Windows.Storage.Streams.IRandomAccessStream stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                using (DataReader reader = new DataReader(stream))
                {
                    byte[] bytes = new byte[stream.Size];
                    reader.ReadBytes(bytes);
                    return bytes;
                }
            }
        }
        else
        {
            return new byte[0];
        }
    }

    /// <summary>
    /// Decodes a <see cref="byte[]"/> to <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/>
    /// via <see cref="Windows.Graphics.Imaging.BitmapDecoder.CreateAsync"/>.
    /// </summary>
    /// <param name="data"><see cref="byte[]"/></param>
    /// <returns><see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/> if successful, null otherwise</returns>
    public static async Task<ImageSource?> ImageFromBase64(byte[] bytes, Guid? decoderId)
    {
        try
        {
            if (decoderId == null)
            {
                decoderId = Windows.Graphics.Imaging.BitmapDecoder.PngDecoderId; // https://learn.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmapdecoder.pngdecoderid?view=winrt-22621
                // [Available decoders]
                //  - GIF
                //  - HEIF
                //  - ICO
                //  - JPEG
                //  - JPEGXR
                //  - PNG
                //  - TIFF
                //  - WEBP
            }

            Windows.Storage.Streams.IRandomAccessStream? image = bytes.AsBuffer().AsStream().AsRandomAccessStream();

            // Create the decoder from the image bytes.
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync((Guid)decoderId, image);
            image.Seek(0);

            // Create writable bitmap from decoder source.
            var output = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap((int)decoder.PixelHeight, (int)decoder.PixelWidth);
            await output.SetSourceAsync(image);

            return output;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ImageFromBase64: {ex.Message}");
            return null;
        }
    }
    #endregion

    #region [Superfluous]
    /// <summary>
    /// Converts an in-memory BitmapImage to a MemoryStream.
    /// </summary>
    public static async Task<MemoryStream> ConvertBitmapImageToMemoryStreamAsync(BitmapImage bitmapImage)
    {
        bool saveToDisk = false;

        if (bitmapImage.PixelWidth == 0 || bitmapImage.PixelHeight == 0)
        {
            throw new InvalidOperationException("BitmapImage has no valid pixel dimensions.");
        }

        // Step 1: Create an in-memory stream
        using (InMemoryRandomAccessStream randomStream = new InMemoryRandomAccessStream())
        {
            // Step 2: Encode BitmapImage into PNG format
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomStream);

            // Step 3: Create an empty SoftwareBitmap (since BitmapImage doesn't allow pixel access)
            SoftwareBitmap softwareBitmap = new SoftwareBitmap(
                BitmapPixelFormat.Bgra8,
                bitmapImage.PixelWidth,
                bitmapImage.PixelHeight,
                BitmapAlphaMode.Premultiplied);

            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();

            // Step 4: Convert to MemoryStream
            MemoryStream finalStream = new MemoryStream();
            await randomStream.AsStreamForRead().CopyToAsync(finalStream);
            finalStream.Position = 0; // Reset position for reading

            if (saveToDisk && softwareBitmap != null)
            {
                await SaveSoftwareBitmapToFileAsync(softwareBitmap, System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "SoftwareBitmap.png"));
            }

            return finalStream;
        }
    }

    /// <summary>
    /// Converts a WriteableBitmap to a MemoryStream.
    /// </summary>
    public static async Task<MemoryStream> ConvertWriteableBitmapToMemoryStreamAsync(WriteableBitmap writeableBitmap)
    {
        if (writeableBitmap.PixelWidth == 0 || writeableBitmap.PixelHeight == 0)
            throw new InvalidOperationException("WriteableBitmap has no valid pixel dimensions.");

        // Step 1: Get pixel buffer
        Stream pixelStream = writeableBitmap.PixelBuffer.AsStream();
        byte[] pixels = new byte[pixelStream.Length];
        await pixelStream.ReadAsync(pixels, 0, pixels.Length);

        // Step 2: Create an in-memory stream
        using (InMemoryRandomAccessStream randomStream = new InMemoryRandomAccessStream())
        {
            // Step 3: Encode as PNG
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomStream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)writeableBitmap.PixelWidth,
                (uint)writeableBitmap.PixelHeight,
                96, 96,  // DPI
                pixels);

            await encoder.FlushAsync();

            // Step 4: Convert to MemoryStream
            MemoryStream finalStream = new MemoryStream();
            await randomStream.AsStreamForRead().CopyToAsync(finalStream);
            finalStream.Position = 0; // Reset position for reading

            return finalStream;
        }
    }

    /// <summary>
    /// Saves a BitmapImage to a file by converting it to a WriteableBitmap first.
    /// </summary>
    public static async Task SaveBitmapImageToFileAsync(BitmapImage bitmapImage, string fileName = "BitmapImage.png")
    {
        if (bitmapImage == null)
        {
            throw new ArgumentNullException(nameof(bitmapImage));
        }

        // Step 1: Convert BitmapImage to WriteableBitmap
        WriteableBitmap writeableBitmap = await ConvertBitmapImageToWriteableBitmapAsync(bitmapImage);

        // Step 2: Extract pixel data
        byte[] pixels = writeableBitmap.PixelBuffer.ToArray();

        // Step 3: Get save location
        Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(Path.Combine(AppContext.BaseDirectory, "Assets", fileName));
        if (file == null)
        {
            throw new Exception("SaveBitmapImageToFile: Storage file problem.");
        }

        // Step 4: Save image to file
        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
        {
            await EncodeAndSaveWriteableBitmapAsync(writeableBitmap, pixels, stream);
        }
    }

    /// <summary>
    /// Converts a BitmapImage to a WriteableBitmap.
    /// </summary>
    public static async Task<WriteableBitmap> ConvertBitmapImageToWriteableBitmapAsync(BitmapImage bitmapImage)
    {
        if (bitmapImage.PixelWidth == 0 || bitmapImage.PixelHeight == 0)
            throw new InvalidOperationException("BitmapImage has no valid pixel dimensions.");

        WriteableBitmap writeableBitmap = new WriteableBitmap(bitmapImage.PixelWidth, bitmapImage.PixelHeight);

        using (InMemoryRandomAccessStream memoryStream = new InMemoryRandomAccessStream())
        {
            // Encode BitmapImage to a stream
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoryStream);
            encoder.SetSoftwareBitmap(new SoftwareBitmap(BitmapPixelFormat.Bgra8, bitmapImage.PixelWidth, bitmapImage.PixelHeight, BitmapAlphaMode.Premultiplied));
            await encoder.FlushAsync();

            // Assign stream to WriteableBitmap
            await writeableBitmap.SetSourceAsync(memoryStream);
        }

        return writeableBitmap;
    }


    /// <summary>
    /// Converts a BitmapImage to a WriteableBitmap.
    /// </summary>
    public static async Task<WriteableBitmap> ConvertBitmapImageToWriteableBitmapIfValidUriSourceAsync(BitmapImage bitmapImage)
    {
        // Step 1: Open BitmapImage as a Stream
        StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(bitmapImage.UriSource);
        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
        {
            // Step 2: Decode image to a WriteableBitmap
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            WriteableBitmap writeableBitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);

            // Step 3: Copy image data into WriteableBitmap
            using (Stream pixelStream = writeableBitmap.PixelBuffer.AsStream())
            {
                byte[] pixels = (await decoder.GetPixelDataAsync()).DetachPixelData();
                await pixelStream.WriteAsync(pixels, 0, pixels.Length);
            }

            return writeableBitmap;
        }
    }

    /// <summary>
    /// Encodes and saves a WriteableBitmap to a file.
    /// </summary>
    public static async Task EncodeAndSaveWriteableBitmapAsync(WriteableBitmap writeableBitmap, byte[] pixels, IRandomAccessStream stream)
    {
        // Step 1: Create BitmapEncoder
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

        // Step 2: Set pixel data
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)writeableBitmap.PixelWidth,
            (uint)writeableBitmap.PixelHeight,
            96, 96, // DPI
            pixels);

        // Step 3: Save the file
        await encoder.FlushAsync();
    }

    /// <summary>
    /// Encodes and saves a RenderTargetBitmap to a file using DataWriter.
    /// </summary>
    public static async Task EncodeAndSaveBitmapWithDataWriterAsync(RenderTargetBitmap renderTargetBitmap, byte[] pixels, IRandomAccessStream stream)
    {
        // Step 1: Create BitmapEncoder
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

        // Step 2: Set pixel data
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            (uint)renderTargetBitmap.PixelWidth,
            (uint)renderTargetBitmap.PixelHeight,
            96, 96, // DPI
            pixels);

        // Step 3: Encode and flush
        await encoder.FlushAsync();

        // Step 4: Write the image file manually using DataWriter
        using (DataWriter dataWriter = new DataWriter(stream))
        {
            dataWriter.WriteBytes(pixels);
            await dataWriter.StoreAsync();
            await dataWriter.FlushAsync();
        }
    }
    #endregion
}
