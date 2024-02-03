﻿namespace WpfRemote.Converters;

using System;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(object), typeof(bool))]
public class NotZeroToBoolConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return !IsZeroToBoolConverter.IsZero(value);
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
