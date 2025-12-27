using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StorageManager.Converters
{
    /// <summary>
    /// Логика взаимодействия для BooleanToVisibilityConverter.cs
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter != null && parameter.ToString().ToLower() == "invert")
                {
                    boolValue = !boolValue;
                }

                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}