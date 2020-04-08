using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DistrictEnergy
{
    /// <summary>
    ///     Resolves the units for a $ quantity
    /// </summary>
    public class CostConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999999)
                    return "M$";
                return "$";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "$";
        }
    }

    public class NormalizedCostConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 999999)
                    return "M$/m²";
                return "$/m²";
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "$";
        }
    }
}