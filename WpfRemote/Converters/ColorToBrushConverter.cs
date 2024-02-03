namespace WpfUtils.Converters;

using System.Windows.Data;
using System.Windows.Media;

[ValueConversion(typeof(Color), typeof(Brush))]
public class ColorToBrushConverter : ConverterBase<Color, Brush>
{
	protected override Brush Convert(Color value)
	{
		return new SolidColorBrush(value);
	}
}
