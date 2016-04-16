using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DarkSoulsCloudSave.ValueConverter
{
    public class InverseBooleanValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Inverse(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Inverse(value);
        }

        private bool Inverse(object value)
        {
            if (value is bool)
                return !((bool)value);
            return value == null;
        }
    }
}
