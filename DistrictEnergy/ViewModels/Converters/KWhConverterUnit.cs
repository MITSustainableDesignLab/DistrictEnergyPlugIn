using System;
using System.Globalization;
using System.Windows.Data;

namespace DistrictEnergy.ViewModels.Converters
{
    /// <summary>
    ///     Converts a kWh qauntity to MWh or GWh depending on magnitude of value
    /// </summary>
    public class KWhConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return "MWh";

                if (d > 999999)
                    return "GWh";
                return "kWh";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "kWh";
        }
    }
}