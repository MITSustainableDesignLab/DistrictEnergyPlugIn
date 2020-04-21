using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DistrictEnergy.ViewModels.Converters
{
    public class KwPerHourConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999)
                    return "MWh / h";

                if (d > 999999)
                    return "GWh / h";
                return "kWh / h";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "kW / h";
        }
    }
}