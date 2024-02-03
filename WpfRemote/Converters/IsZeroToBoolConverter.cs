namespace WpfUtils.Converters;

using System;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(object), typeof(bool))]
public class IsZeroToBoolConverter : IValueConverter
{
	public static bool IsZero(object value)
	{
		if (value is int intV)
		{
			return intV == 0;
		}
		else if (value is float floatV)
		{
			return floatV == 0;
		}
		else if (value is double doubleV)
		{
			return doubleV == 0;
		}
		else if (value is uint uintV)
		{
			return uintV == 0;
		}
		else if (value is ushort ushortV)
		{
			return ushortV == 0;
		}
		else if (value is byte byteV)
		{
			return byteV == 0;
		}
		else
		{
			throw new NotImplementedException($"value type {value.GetType()} not supported for not zero converter");
		}
	}

	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return IsZeroToBoolConverter.IsZero(value);
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
