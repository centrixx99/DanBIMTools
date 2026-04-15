using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace DanBIMTools.Core
{
    /// <summary>
    /// Helper class for loading ribbon icons from embedded resources.
    /// </summary>
    public static class IconHelper
    {
        /// <summary>
        /// Loads an icon from embedded resources.
        /// Returns null if not found (Revit will display default button).
        /// </summary>
        public static BitmapImage? LoadIcon(string iconName, int size = 32)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"DanBIMTools.Resources.Icons.{iconName}_{size}.png";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                Stream? iconStream = stream;
                
                if (iconStream == null)
                {
                    // Try without size suffix
                    resourceName = $"DanBIMTools.Resources.Icons.{iconName}.png";
                    iconStream = assembly.GetManifestResourceStream(resourceName);
                }

                if (iconStream != null)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = iconStream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                // Log error if needed, but silently fail for icons
                System.Diagnostics.Debug.WriteLine($"Failed to load icon {iconName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Loads an icon from a file path.
        /// Useful for loading custom user icons.
        /// </summary>
        public static BitmapImage? LoadIconFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load icon from {filePath}: {ex.Message}");
            }

            return null;
        }
    }
}
