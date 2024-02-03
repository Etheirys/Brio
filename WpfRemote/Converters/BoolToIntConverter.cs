namespace WpfUtils.Converters;

public class BoolToIntConverter : ConverterBase<bool, int>
{
	protected override int Convert(bool value)
	{
		return value ? 1 : 0;
	}
}
