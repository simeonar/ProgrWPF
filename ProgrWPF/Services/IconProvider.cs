using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProgrWPF.UI
{
    public static class IconProvider
    {
        public static FrameworkElement GetIcon(string sectionName)
        {
            string iconPath = GetIconPath(sectionName);
            if (!string.IsNullOrEmpty(iconPath))
            {
                var image = new Image
                {
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(0, 5, 0, 5),
                    Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute))
                };
                return image;
            }

            // Прозрачный placeholder
            var placeholder = new Border
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 5, 0, 5),
                Background = Brushes.Transparent
            };
            return placeholder;
        }

        private static string GetIconPath(string sectionName)
        {
            string basePath = "pack://application:,,,/ProgrWPF;component/Services/Icons/";

            if (sectionName == "Rules")
                return basePath + "amsn.png";
            if (sectionName == "Model")
                return basePath + "amsn.png";
            if (sectionName == "Part")
                return basePath + "amsn.png";
            if (sectionName == "Align")
                return basePath + "amsn.png";
            if (sectionName == "Measurement")
                return basePath + "amsn.png";

            return null;
        }
    }
}
