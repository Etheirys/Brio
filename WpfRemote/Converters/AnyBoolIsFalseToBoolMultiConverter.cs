namespace WpfUtils.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

/// <summary>
/// If all of the bools are true, returns false.
/// If any of the bools are false, returns true.
/// </summary>
public class AnyBoolIsFalseToBoolMultiConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		foreach (object value in values)
		{
			if (value is bool boolValue)
			{
				if (!boolValue)
				{
					return true;
				}
			}
		}

		return false;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException("BooleanAndConverter is a OneWay converter.");
	}
}
