using System;
using Windows.UI.Xaml.Data;

namespace DialIn.VideoEffects.Uwp.Converters
{
    public class ObjectToFloatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (float) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (object) value;
        }
    }
}
