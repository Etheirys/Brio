namespace WpfUtils.Converters;

using System;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(object), typeof(Visibility))]
public class IsZeroToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return IsZeroToBoolConverter.IsZero(value) ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
