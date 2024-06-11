using System;
using System.Globalization;
using System.Windows.Data;

namespace CtrlWin
{
    public class FilePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    var uri = new Uri(path, UriKind.Absolute);
                    Console.WriteLine($"Converted path to URI: {uri}");  // Debugging output
                    return uri;
                }
                catch (UriFormatException ex)
                {
                    Console.WriteLine($"Invalid URI format: {ex.Message}");  // Debugging output
                    return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
