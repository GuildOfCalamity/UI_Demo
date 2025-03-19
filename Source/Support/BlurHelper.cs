using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace UI_Demo;

public static class BlurHelper
{
    /// <summary>
    ///   Applies a simple box blur to a <see cref="BitmapImage"/> and returns a new blurred <see cref="BitmapImage"/>.
    /// </summary>
    /// <param name="bitmapImage">The input image to blur.</param>
    /// <param name="blurRadius">The blur intensity (higher = more blur).</param>
    /// <returns>A blurred <see cref="BitmapImage"/></returns>
    public static async Task<BitmapImage> ApplyBlurAsync(BitmapImage bitmapImage, int blurRadius = 6)
    {
        // Convert BitmapImage to a SoftwareBitmap
        SoftwareBitmap softwareBitmap = await ConvertBitmapImageToSoftwareBitmapAsync(bitmapImage);

        // Get pixel data
        int width = softwareBitmap.PixelWidth;
        int height = softwareBitmap.PixelHeight;
        byte[] pixelData = new byte[4 * width * height]; // BGRA8 format

        softwareBitmap.CopyToBuffer(pixelData.AsBuffer());

        // Apply box blur effect
        byte[] blurredPixels = ApplyBoxBlur(pixelData, width, height, blurRadius);

        // Create a new SoftwareBitmap from blurred pixels
        SoftwareBitmap blurredBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
        blurredBitmap.CopyFromBuffer(blurredPixels.AsBuffer());

        // Convert back to BitmapImage
        return await ConvertSoftwareBitmapToBitmapImageAsync(blurredBitmap);
    }

    /// <summary>
    /// Applies a simple box blur to a <see cref="BitmapImage"/> and returns a new blurred <see cref="BitmapImage"/>.
    /// </summary>
    /// <param name="softwareBitmap">The input image to blur.</param>
    /// <param name="blurRadius">The blur intensity (higher = more blur).</param>
    /// <returns>A blurred <see cref="BitmapImage"/></returns>
    public static async Task<BitmapImage> ApplyBlurAsync(SoftwareBitmap softwareBitmap, int blurRadius = 6)
    {
        // Get the pixel data.
        int width = softwareBitmap.PixelWidth;
        int height = softwareBitmap.PixelHeight;
        byte[] pixelData = new byte[4 * width * height]; // BGRA8 format

        softwareBitmap.CopyToBuffer(pixelData.AsBuffer());

        // Apply box blur effect.
        byte[] blurredPixels = ApplyBoxBlur(pixelData, width, height, blurRadius);

        // Create a new SoftwareBitmap from blurred pixels.
        SoftwareBitmap blurredBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
        blurredBitmap.CopyFromBuffer(blurredPixels.AsBuffer());

        // Convert back to BitmapImage.
        return await ConvertSoftwareBitmapToBitmapImageAsync(blurredBitmap);
    }

    /// <summary>
    /// Applies a simple box blur to a <see cref="BitmapImage"/> and returns a new blurred <see cref="SoftwareBitmap"/>.
    /// </summary>
    /// <param name="softwareBitmap">The input image to blur.</param>
    /// <param name="blurRadius">The blur intensity (higher = more blur).</param>
    /// <returns>A blurred <see cref="BitmapImage"/></returns>
    public static SoftwareBitmap ApplyBlur(SoftwareBitmap softwareBitmap, int blurRadius = 6)
    {
        // Get the pixel data.
        int width = softwareBitmap.PixelWidth;
        int height = softwareBitmap.PixelHeight;
        byte[] pixelData = new byte[4 * width * height]; // BGRA8 format

        softwareBitmap.CopyToBuffer(pixelData.AsBuffer());

        // Apply box blur effect.
        byte[] blurredPixels = ApplyBoxBlur(pixelData, width, height, blurRadius);

        // Create a new SoftwareBitmap from blurred pixels.
        SoftwareBitmap blurredBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
        blurredBitmap.CopyFromBuffer(blurredPixels.AsBuffer());

        // Convert back to BitmapImage.
        return blurredBitmap;
    }

    /// <summary>
    /// Applies a simple box blur to a <see cref="BitmapImage"/> and saves the <see cref="SoftwareBitmap"/> to disk.
    /// </summary>
    /// <param name="softwareBitmap">The input image to blur.</param>
    /// <param name="blurRadius">The blur intensity (higher = more blur).</param>
    /// <param name="filePath">The output path to save.</param>
    /// <returns>A blurred <see cref="BitmapImage"/></returns>
    public static async Task<bool> ApplyBlurAndSaveAsync(SoftwareBitmap softwareBitmap, string filePath, int blurRadius = 6)
    {
        // Get the pixel data.
        int width = softwareBitmap.PixelWidth;
        int height = softwareBitmap.PixelHeight;
        byte[] pixelData = new byte[4 * width * height]; // BGRA8 format

        softwareBitmap.CopyToBuffer(pixelData.AsBuffer());

        // Apply box blur effect.
        byte[] blurredPixels = ApplyBoxBlur(pixelData, width, height, blurRadius);

        // Create a new SoftwareBitmap from blurred pixels.
        SoftwareBitmap blurredBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
        blurredBitmap.CopyFromBuffer(blurredPixels.AsBuffer());

        if (string.IsNullOrEmpty(filePath))
            return false;

        return await AppCapture.SaveSoftwareBitmapToFileAsync(blurredBitmap, filePath);
    }

    /// <summary>
    /// Applies a simple box blur algorithm to pixel data.
    /// </summary>
    static byte[] ApplyBoxBlur(byte[] pixels, int width, int height, int radius)
    {
        byte[] result = new byte[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int r = 0, g = 0, b = 0, a = 0, count = 0;
                // Iterate over neighboring pixels
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            int index = (ny * width + nx) * 4;
                            b += pixels[index + 0]; // Blue
                            g += pixels[index + 1]; // Green
                            r += pixels[index + 2]; // Red
                            a += pixels[index + 3]; // Alpha
                            count++;
                        }
                    }
                }
                // Compute average color
                int outputIndex = (y * width + x) * 4;
                result[outputIndex + 0] = (byte)(b / count);
                result[outputIndex + 1] = (byte)(g / count);
                result[outputIndex + 2] = (byte)(r / count);
                result[outputIndex + 3] = (byte)(a / count);
            }
        }
        return result;
    }

    /// <summary>
    /// Converts a BitmapImage to a SoftwareBitmap.
    /// </summary>
    public static async Task<SoftwareBitmap> ConvertBitmapImageToSoftwareBitmapAsync(BitmapImage bitmapImage)
    {
        using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            return await decoder.GetSoftwareBitmapAsync();
        }
    }

    /// <summary>
    /// Converts a SoftwareBitmap to a BitmapImage.
    /// </summary>
    public static async Task<BitmapImage> ConvertSoftwareBitmapToBitmapImageAsync(SoftwareBitmap softwareBitmap)
    {
        using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetSoftwareBitmap(softwareBitmap);
            await encoder.FlushAsync();
            BitmapImage bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(stream);
            return bitmapImage;
        }
    }

    public static async Task<SoftwareBitmap> LoadFromFile(StorageFile file)
    {
        SoftwareBitmap softwareBitmap;
        using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
        }
        return softwareBitmap;
    }

    public static async Task<SoftwareBitmap?> LoadSoftwareBitmapFromUriAsync(Uri uri)
    {
        try
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                return softwareBitmap;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSoftwareBitmapFromUriAsync: {ex.Message}");
            return null;
        }
    }

    public static async Task<SoftwareBitmap?> LoadSoftwareBitmapFromPathAsync(string path)
    {
        try
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                return softwareBitmap;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] LoadSoftwareBitmapFromPathAsync: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Sample method for testing blur helper.
/// </summary>
//async void ApplyBoxBlurTest()
//{
//    // Load image from assets.
//    BitmapImage originalImage = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"));
//    // Apply blur
//    BitmapImage blurredImage = await ImageProcessingHelper.ApplyHomeBrewBlurAsync(originalImage, blurRadius: 5);
//    // Display the blurred image in an Image control
//    MyImageControl.Source = blurredImage;
//}

