using System;
using System.Globalization;
using System.Windows.Data;

namespace DistrictEnergy.ViewModels.Converters
{
    /// <summary>
    ///     Converts a kWh quantity to MWh or GWh depending on magnitude of value
    /// </summary>
    public class kgCO2ConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return "tCO2eq";

                if (d > 999999)
                    return "MtCO2eq";
                return "kgCO2eq";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "kWh";
        }
    }
}