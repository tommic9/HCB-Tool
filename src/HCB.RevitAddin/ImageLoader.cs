using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HCB.RevitAddin
{
    internal static class ImageLoader
    {
        public static ImageSource LoadPng(string path, int pixelWidth)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
            image.DecodePixelWidth = pixelWidth;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
