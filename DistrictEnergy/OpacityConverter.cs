using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DistrictEnergy
{
    public class OpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 1d;
            return (Visibility) value == Visibility.Visible // todo this doesn't work yet
                ? 1d
                : .2d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}