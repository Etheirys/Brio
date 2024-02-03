namespace WpfUtils.Controls;

using System.Reflection;
using System.Windows.Input;

public class Slider : System.Windows.Controls.Slider
{
	private MethodInfo? moveToNextTickMethod;

	public Slider()
	{
		this.PreviewKeyDown += this.OnPreviewKeyDown;
		this.PreviewKeyUp += this.OnPreviewKeyUp;
	}

	protected double GetChangeMultiplier()
	{
		if (Keyboard.IsKeyDown(Key.LeftShift))
			return 10;

		if (Keyboard.IsKeyDown(Key.RightShift))
			return 10;

		if (Keyboard.IsKeyDown(Key.LeftCtrl))
			return 0.1f;

		if (Keyboard.IsKeyDown(Key.RightCtrl))
			return 0.1f;

		return 1.0;
	}

	protected override void OnDecreaseSmall()
	{
		this.MoveToNextTick(-this.SmallChange * this.GetChangeMultiplier());
	}

	protected override void OnIncreaseSmall()
	{
		this.MoveToNextTick(this.SmallChange * this.GetChangeMultiplier());
	}

	protected override void OnDecreaseLarge()
	{
		this.MoveToNextTick(-this.LargeChange * this.GetChangeMultiplier());
	}

	protected override void OnIncreaseLarge()
	{
		this.MoveToNextTick(this.LargeChange * this.GetChangeMultiplier());
	}

	protected void MoveToNextTick(double direction)
	{
		if (this.moveToNextTickMethod == null)
			this.moveToNextTickMethod = typeof(System.Windows.Controls.Slider).GetMethod("MoveToNextTick", BindingFlags.NonPublic | BindingFlags.Instance);

		if (this.moveToNextTickMethod == null)
			return;

		this.moveToNextTickMethod.Invoke(this, new object[] { direction });
	}

	protected virtual void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Left)
		{
			this.OnDecreaseSmall();
			e.Handled = true;
		}
		else if (e.Key == Key.Right)
		{
			this.OnIncreaseSmall();
			e.Handled = true;
		}
		else if (e.Key == Key.Down)
		{
			this.MoveFocus(new(FocusNavigationDirection.Next));
			e.Handled = true;
		}
		else if (e.Key == Key.Up)
		{
			this.MoveFocus(new(FocusNavigationDirection.Previous));
			e.Handled = true;
		}
	}

	protected virtual void OnPreviewKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.Up)
		{
			e.Handled = true;
			this.Value = 0;
		}
	}
}
