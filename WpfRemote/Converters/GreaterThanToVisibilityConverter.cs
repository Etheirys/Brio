namespace WpfUtils.Converters;

using System.Windows;

public class GreaterThanToVisibilityConverter : ConverterBase<double, Visibility, double>
{
	protected override Visibility Convert(double value)
	{
		return value > this.Parameter ? Visibility.Visible : Visibility.Collapsed;
	}
}
