using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HCB.RevitAddin
{
    internal static class ImageLoader
    {
        private static readonly ConcurrentDictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static ImageSource LoadPng(string path, int pixelWidth)
        {
            string cacheKey = $"{Path.GetFullPath(path)}|{pixelWidth}";
            return Cache.GetOrAdd(cacheKey, _ =>
            {
                BitmapImage image = new();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
                image.DecodePixelWidth = pixelWidth;
                image.EndInit();
                image.Freeze();
                return image;
            });
        }
    }
}
