namespace WpfUtils.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

[ValueConversion(typeof(object), typeof(double))]
public class NumberConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double doubleVal && parameter is Type target)
		{
			if (target == typeof(byte))
				return System.Convert.ToByte(value);

			if (target == typeof(short))
				return System.Convert.ToInt16(value);

			if (target == typeof(int))
				return System.Convert.ToInt32(value);

			if (target == typeof(long))
				return System.Convert.ToInt64(value);

			if (target == typeof(float))
				return System.Convert.ToSingle(value);
		}

		return value;
	}
}
