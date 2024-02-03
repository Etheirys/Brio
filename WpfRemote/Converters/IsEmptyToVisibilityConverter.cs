namespace WpfUtils.Converters;

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(IEnumerable), typeof(Visibility))]
public class IsEmptyToVisibilityConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null)
			return Visibility.Visible;

		if (value is IEnumerable enumerable)
		{
			foreach (object obj in enumerable)
			{
				return Visibility.Collapsed;
			}
		}

		return Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
