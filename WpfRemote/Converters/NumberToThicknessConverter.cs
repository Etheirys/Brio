namespace WpfUtils.Converters;

using System.Windows;

public class NumberToThicknessLeftConverter : NumberToThicknessConverter
{
	protected override void Add(double value, ref Thickness baseThickness)
	{
		baseThickness.Left += value;
	}
}

public class NumberToThicknessTopConverter : NumberToThicknessConverter
{
	protected override void Add(double value, ref Thickness baseThickness)
	{
		baseThickness.Top += value;
	}
}

public class NumberToThicknessRightConverter : NumberToThicknessConverter
{
	protected override void Add(double value, ref Thickness baseThickness)
	{
		baseThickness.Right += value;
	}
}

public class NumberToThicknessBottomConverter : NumberToThicknessConverter
{
	protected override void Add(double value, ref Thickness baseThickness)
	{
		baseThickness.Bottom += value;
	}
}

public abstract class NumberToThicknessConverter : ConverterBase<double, Thickness>
{
	private static readonly ThicknessConverter ThicknessConverter = new();

	protected sealed override Thickness Convert(double value)
	{
		Thickness thickness = default;

		if (this.Parameter is string paramStr)
		{
			object? obj = ThicknessConverter.ConvertFrom(this.Parameter);
			if (obj is Thickness thicknessParam)
			{
				thickness = thicknessParam;
			}
		}

		this.Add(value, ref thickness);
		return thickness;
	}

	protected abstract void Add(double value, ref Thickness thickness);
}