using System;
using System.IO;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace RaytolfasLauncher
{
    public static class ImageBlurHelper
    {
        public static Bitmap? BlurImage(Stream inputStream, float blurSigma = 20f)
        {
            try
            {
                using var originalBitmap = SKBitmap.Decode(inputStream);
                if (originalBitmap == null) return null;

                int targetWidth = Math.Min(originalBitmap.Width, 1280);
                int targetHeight = (int)((float)targetWidth / originalBitmap.Width * originalBitmap.Height);

                var imageInfo = new SKImageInfo(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var surface = SKSurface.Create(imageInfo);
                if (surface == null) return null;

                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                using var paint = new SKPaint();
                using var filter = SKImageFilter.CreateBlur(blurSigma, blurSigma);
                paint.ImageFilter = filter;

                canvas.DrawBitmap(originalBitmap, new SKRect(0, 0, targetWidth, targetHeight), paint);
                canvas.Flush();

                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 85);
                using var memStream = new MemoryStream();
                data.SaveTo(memStream);
                memStream.Position = 0;

                return new Bitmap(memStream);
            }
            catch
            {
                return null;
            }
        }

        public static Bitmap? BlurImageFile(string filePath, float blurSigma = 20f)
        {
            if (!File.Exists(filePath)) return null;
            using var stream = File.OpenRead(filePath);
            return BlurImage(stream, blurSigma);
        }
    }
}
