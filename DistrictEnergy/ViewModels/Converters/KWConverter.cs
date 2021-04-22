using System;
using System.Globalization;
using System.Windows.Data;

namespace DistrictEnergy.ViewModels.Converters
{
    /// <summary>
    ///     Converts a kW quantity to MWh or GWh depending on magnitude of value
    /// </summary>
    public class KWConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return (d / 1000).ToString("N0", culture); // for MW

                if (d > 999999)
                    return (d / 1000000).ToString("N0", culture); // for GW
                return d.ToString("N0", culture); // for kW;
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