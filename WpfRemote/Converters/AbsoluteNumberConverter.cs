namespace WpfUtils.Converters;

using System;

public class AbsoluteNumberConverter : ConverterBase<double, double, double>
{
	protected override double Convert(double value)
	{
		return Math.Abs(value);
	}
}
