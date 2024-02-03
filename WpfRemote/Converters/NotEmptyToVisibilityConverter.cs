namespace WpfUtils.Converters;

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(IEnumerable), typeof(Visibility))]
public class NotEmptyToVisibilityConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
			return Visibility.Collapsed;

		if (value is IEnumerable enumerable)
		{
			foreach (object obj in enumerable)
			{
				return Visibility.Visible;
			}
		}

		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
