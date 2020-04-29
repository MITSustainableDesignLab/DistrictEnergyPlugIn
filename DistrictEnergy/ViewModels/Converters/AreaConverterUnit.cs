using System;
using System.Globalization;
using System.Windows.Data;

namespace DistrictEnergy.ViewModels.Converters
{
    /// <summary>
    ///     If value is higher than 999999 m2, than show square km2
    /// </summary>
    public class AreaConverterUnit : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
                if (d > 999999)
                    return "km²"; // km^2
            return "m²";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}