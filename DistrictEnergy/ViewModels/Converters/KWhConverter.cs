using System;
using System.Globalization;
using System.Windows.Data;

namespace DistrictEnergy.ViewModels.Converters
{
    /// <summary>
    ///     Converts a kWh quantity to MWh or GWh depending on magnitude of value
    /// </summary>
    public class KWhConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return (d / 1000).ToString("N1", culture); // for MWh

                if (d > 999999)
                    return (d / 1000000).ToString("N1", culture); // for GWh
                return d.ToString("N1", culture); // for kWh;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double d;
            if (double.TryParse((string) value, out d))
                return d;
            return 0.0;
        }
    }
}