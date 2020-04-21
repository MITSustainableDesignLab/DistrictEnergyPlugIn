using System;
using System.Globalization;
using System.Windows.Data;

namespace DistrictEnergy.ViewModels.Converters
{
    /// <summary>
    ///     Converts a $ quantity to M$ depending on magnitude of value
    /// </summary>
    public class DollarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999999)
                    return (d / 1000000).ToString("N", culture); // for M$
                return d.ToString("N", culture); // for $;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d;
            if (double.TryParse((string)value, out d))
                return d;
            return 0.0;
        }
    }
}