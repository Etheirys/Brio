namespace WpfUtils.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(Enum), typeof(Visibility))]
public class EnumToVisibilityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		return EnumToBoolConverter.Convert(value, targetType, parameter) ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
