namespace WpfUtils.Converters;

using System;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(object), typeof(Visibility))]
public class NotZeroToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return IsZeroToBoolConverter.IsZero(value) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
