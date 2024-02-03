namespace WpfUtils.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

[ValueConversion(typeof(Enum), typeof(bool))]
public class EnumToBoolConverter : IValueConverter
{
	private enum AdditionMode
	{
		Or,
		And,
	}

	public static bool Convert(object? value, Type targetType, object parameter)
	{
		if (value == null)
			return false;

		Type enumType = value.GetType();

		if (!enumType.IsEnum)
			throw new Exception("Enum converter can only be used on an enum type");

		Enum currentValue = (Enum)value;

		string enumValueString = (string)parameter;

		AdditionMode mode = AdditionMode.Or;

		if (enumValueString.Contains("|") && enumValueString.Contains("&"))
		{
			throw new NotSupportedException("Cannot mix or (|) with and (&) in enum converter parameter");
		}
		else if (enumValueString.Contains("|"))
		{
			mode = AdditionMode.Or;
		}
		else if (enumValueString.Contains("&"))
		{
			mode = AdditionMode.And;
		}

		string[] values = enumValueString.Split('|', '?', StringSplitOptions.RemoveEmptyEntries);
		bool returnvalue = false;

		foreach (string enumValueStringPart in values)
		{
			Enum parameterValue = (Enum)Enum.Parse(enumType, enumValueStringPart.Trim(' ', '!'));

			bool isEnumValue = Enum.Equals(currentValue, parameterValue);

			if (enumValueString.StartsWith('!'))
				isEnumValue = !isEnumValue;

			if (mode == AdditionMode.Or)
			{
				returnvalue |= isEnumValue;
			}
			else
			{
				returnvalue &= isEnumValue;
			}
		}

		return returnvalue;
	}

	public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
	{
		return Convert(value, targetType, parameter);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is not bool bVal || bVal == false)
			return Binding.DoNothing;

		if (!targetType.IsEnum)
			throw new Exception("Enum converter can only be used on an enum type");

		return Enum.Parse(targetType, (string)parameter);
	}
}
