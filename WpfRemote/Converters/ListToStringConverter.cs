namespace WpfUtils.Converters;

using System;
using System.Collections;
using System.Windows.Data;

[ValueConversion(typeof(IEnumerable), typeof(string))]
public class ListToStringConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		if (value is IEnumerable enumerable)
		{
			string str = string.Empty;
			int count = 0;
			foreach (object v in enumerable)
			{
				str += v.ToString() + ", ";
				count++;
			}

			return count + ": " + str.TrimEnd(' ', ',');
		}

		throw new Exception("List to string converter can only be used with enumerable sources");
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
