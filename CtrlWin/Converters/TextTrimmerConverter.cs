using System;
using System.Globalization;
using System.Windows.Data;

namespace CtrlWin.Converters
{
    public class TextTrimmerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? text = value as string;
            if (string.IsNullOrEmpty(text))
                return text;

            int maxLength = 50; // Limit to 50 characters
            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
