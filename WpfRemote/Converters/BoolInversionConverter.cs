namespace WpfUtils.Converters;

public class BoolInversionConverter : ConverterBase<bool, bool>
{
	protected override bool Convert(bool value)
	{
		return !value;
	}

	protected override bool ConvertBack(bool value)
	{
		return !value;
	}
}
