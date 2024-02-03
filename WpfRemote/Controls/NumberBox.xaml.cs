﻿namespace WpfUtils.Controls;

using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PropertyChanged.SourceGenerator;
using WpfUtils.DependencyProperties;
using DrawPoint = System.Drawing.Point;
using WinCur = System.Windows.Forms.Cursor;
using WinPoint = System.Windows.Point;

/// <summary>
/// Interaction logic for NumberBox.xaml.
/// </summary>
public partial class NumberBox : UserControl, INotifyPropertyChanged
{
	public static readonly IBind<double> ValueDp = Binder.Register<double, NumberBox>(nameof(Value), OnValueChanged);
	public static readonly IBind<double> TickDp = Binder.Register<double, NumberBox>(nameof(TickFrequency), OnTickChanged, BindMode.OneWay);
	public static readonly IBind<SliderModes> SliderDp = Binder.Register<SliderModes, NumberBox>(nameof(Slider), OnSliderChanged, BindMode.OneWay);
	public static readonly IBind<bool> ButtonsDp = Binder.Register<bool, NumberBox>(nameof(Buttons), OnButtonsChanged, BindMode.OneWay);
	public static readonly IBind<double> MinDp = Binder.Register<double, NumberBox>(nameof(Minimum), OnMinimumChanged, BindMode.OneWay);
	public static readonly IBind<double> MaxDp = Binder.Register<double, NumberBox>(nameof(Maximum), OnMaximumChanged, BindMode.OneWay);
	public static readonly IBind<bool> WrapDp = Binder.Register<bool, NumberBox>(nameof(Wrap), BindMode.OneWay);
	public static readonly IBind<double> OffsetDp = Binder.Register<double, NumberBox>(nameof(ValueOffset), BindMode.OneWay);
	public static readonly IBind<bool> UncapTextInputDp = Binder.Register<bool, NumberBox>(nameof(UncapTextInput), BindMode.OneWay);
	public static readonly IBind<object> PrefixDp = Binder.Register<object, NumberBox>(nameof(Prefix), BindMode.OneWay);
	public static readonly IBind<object> SuffixDp = Binder.Register<object, NumberBox>(nameof(Suffix), BindMode.OneWay);
	public static readonly IBind<CornerRadius> CornerRadiusDp = Binder.Register<CornerRadius, NumberBox>(nameof(CornerRadius), OnCornerRadiusChanged, BindMode.OneWay);

	private string? inputString;
	private Key keyHeld = Key.None;
	private double relativeSliderStart;
	private double relativeSliderCurrent;
	private bool bypassFocusLock = false;

	public NumberBox()
	{
		this.InitializeComponent();
		this.TickFrequency = 1;
		this.Minimum = double.MinValue;
		this.Maximum = double.MaxValue;
		this.Wrap = false;
		this.Text = this.DisplayValue.ToString();
		this.Slider = SliderModes.None;
		this.Buttons = false;
		this.CornerRadius = new(6, 6, 6, 6);

		this.ContentArea.DataContext = this;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public enum SliderModes
	{
		None,
		Absolute,
		Relative,
	}

	public double TickFrequency
	{
		get => TickDp.Get(this);
		set => TickDp.Set(this, value);
	}

	public SliderModes Slider
	{
		get => SliderDp.Get(this);
		set => SliderDp.Set(this, value);
	}

	public bool Buttons
	{
		get => ButtonsDp.Get(this);
		set => ButtonsDp.Set(this, value);
	}

	public double Minimum
	{
		get => MinDp.Get(this);
		set => MinDp.Set(this, value);
	}

	public double Maximum
	{
		get => MaxDp.Get(this);
		set => MaxDp.Set(this, value);
	}

	public bool Wrap
	{
		get => WrapDp.Get(this);
		set => WrapDp.Set(this, value);
	}

	public double ValueOffset
	{
		get => OffsetDp.Get(this);
		set => OffsetDp.Set(this, value);
	}

	public double Value
	{
		get => ValueDp.Get(this);
		set => ValueDp.Set(this, value);
	}

	public bool UncapTextInput
	{
		get => UncapTextInputDp.Get(this);
		set => UncapTextInputDp.Set(this, value);
	}

	public object Prefix
	{
		get => PrefixDp.Get(this);
		set => PrefixDp.Set(this, value);
	}

	public object Suffix
	{
		get => SuffixDp.Get(this);
		set => SuffixDp.Set(this, value);
	}

	public CornerRadius CornerRadius
	{
		get => CornerRadiusDp.Get(this);
		set => CornerRadiusDp.Set(this, value);
	}

	public double DisplayValue
	{
		get
		{
			return this.Value + this.ValueOffset;
		}

		set
		{
			this.Value = value - this.ValueOffset;

			if (!this.UncapTextInput)
			{
				if (this.Wrap)
				{
					double range = this.Maximum - this.Minimum;

					if (this.Value > this.Maximum)
						this.Value = this.Minimum + ((this.Value - this.Maximum) % range);

					if (this.Value < this.Minimum)
						this.Value = this.Maximum - ((this.Maximum - this.Value) % range);
				}

				this.Value = Math.Max(this.Minimum, this.Value);
				this.Value = Math.Min(this.Maximum, this.Value);
			}

			this.PropertyChanged?.Invoke(this, new(nameof(NumberBox.DisplayValue)));
		}
	}

	public string? Text
	{
		get
		{
			return this.inputString;
		}

		set
		{
			this.inputString = value;

			double val;
			if (double.TryParse(value, out val))
			{
				this.DisplayValue = val;
				this.ErrorDisplay.Visibility = Visibility.Collapsed;
			}
			else
			{
				try
				{
					val = Convert.ToDouble(new DataTable().Compute(value, null));
					this.DisplayValue = val;
					this.ErrorDisplay.Visibility = Visibility.Collapsed;
				}
				catch (Exception)
				{
					this.ErrorDisplay.Visibility = Visibility.Visible;
				}
			}

			this.PropertyChanged?.Invoke(this, new(nameof(NumberBox.Text)));
		}
	}

	public double SliderValue
	{
		get
		{
			if (this.Slider == SliderModes.Absolute)
			{
				return this.DisplayValue;
			}
			else
			{
				return this.relativeSliderCurrent;
			}
		}
		set
		{
			this.bypassFocusLock = true;

			if (this.Slider == SliderModes.Absolute)
			{
				this.DisplayValue = value;
			}
			else
			{
				this.relativeSliderCurrent = value;

				if (Keyboard.IsKeyDown(Key.LeftShift))
					value *= 10;

				if (Keyboard.IsKeyDown(Key.LeftCtrl))
					value /= 10;

				this.DisplayValue = this.relativeSliderStart + value;
			}

			this.bypassFocusLock = false;

			this.PropertyChanged?.Invoke(this, new(nameof(NumberBox.SliderValue)));
		}
	}

	public double SliderMinimum
	{
		get
		{
			if (this.Slider == SliderModes.Absolute)
			{
				return this.Minimum;
			}
			else
			{
				return -(this.TickFrequency * 30);
			}
		}
	}

	public double SliderMaximum
	{
		get
		{
			if (this.Slider == SliderModes.Absolute)
			{
				return this.Maximum;
			}
			else
			{
				return this.TickFrequency * 30;
			}
		}
	}

	protected override void OnPreviewKeyDown(KeyEventArgs e)
	{
		bool focused = this.InputBox.IsKeyboardFocused || this.InputSlider.IsKeyboardFocused;
		if (!focused)
			return;

		if (e.Key == Key.Return)
		{
			this.Commit(true);
			e.Handled = true;
		}

		if (e.Key == Key.Up || e.Key == Key.Down)
		{
			e.Handled = true;

			if (e.IsRepeat)
			{
				if (this.keyHeld == e.Key)
					return;

				this.keyHeld = e.Key;
				Task.Run(this.TickHeldKey);
			}
			else
			{
				this.TickKey(e.Key);
			}
		}
	}

	protected override void OnPreviewKeyUp(KeyEventArgs e)
	{
		if (this.keyHeld == e.Key)
		{
			e.Handled = true;
			this.keyHeld = Key.None;
		}
	}

	protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
	{
		if (!this.InputBox.IsFocused)
			return;

		e.Handled = true;
		this.TickValue(e.Delta > 0);
	}

	private static void OnValueChanged(NumberBox sender, double v)
	{
		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderValue)));

		if (!sender.bypassFocusLock && sender.InputBox.IsFocused)
			return;

		sender.Text = sender.DisplayValue.ToString("0.###");
	}

	private static void OnSliderChanged(NumberBox sender, SliderModes mode)
	{
		sender.SliderArea.Visibility = mode != SliderModes.None ? Visibility.Visible : Visibility.Collapsed;
		sender.BoxBorder.CornerRadius = mode != SliderModes.None ? new(0, sender.CornerRadius.TopRight, sender.CornerRadius.BottomRight, 0) : sender.CornerRadius;
		sender.SliderArea.CornerRadius = new(sender.CornerRadius.TopLeft, 0, 0, sender.CornerRadius.BottomLeft);

		int inputColumnWidth = 64;
		if (sender.Buttons)
			inputColumnWidth += 48;

		sender.SliderColumn.Width = mode != SliderModes.None ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
		sender.InputBoxColumn.Width = mode != SliderModes.None ? new GridLength(inputColumnWidth) : new GridLength(1, GridUnitType.Star);

		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderMaximum)));
		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderMinimum)));
		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderValue)));
	}

	private static void OnCornerRadiusChanged(NumberBox sender, CornerRadius value)
	{
		sender.BoxBorder.CornerRadius = sender.Slider != SliderModes.None ? new(0, sender.CornerRadius.TopRight, sender.CornerRadius.BottomRight, 0) : sender.CornerRadius;
		sender.SliderArea.CornerRadius = new(sender.CornerRadius.TopLeft, 0, 0, sender.CornerRadius.BottomLeft);
	}

	private static void OnButtonsChanged(NumberBox sender, bool v)
	{
		sender.DownButton.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
		sender.UpButton.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
	}

	private static void OnMinimumChanged(NumberBox sender, double value)
	{
		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderMinimum)));
	}

	private static void OnMaximumChanged(NumberBox sender, double value)
	{
		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderMaximum)));
	}

	private static void OnTickChanged(NumberBox sender, double tick)
	{
		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderMaximum)));
		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderMinimum)));
		sender.PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(NumberBox.SliderValue)));
	}

	private void UserControl_Loaded(object sender, RoutedEventArgs e)
	{
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			window.MouseDown += this.OnWindowMouseDown;
			window.Deactivated += this.OnWindowDeactivated;
		}

		OnSliderChanged(this, this.Slider);
		OnButtonsChanged(this, this.Buttons);
		OnTickChanged(this, this.TickFrequency);

		this.Text = this.DisplayValue.ToString("0.###");
	}

	private double Validate(double v)
	{
		if (this.Wrap)
		{
			if (v > this.Maximum)
			{
				v = this.Minimum;
			}

			if (v < this.Minimum)
			{
				v = this.Maximum;
			}
		}
		else
		{
			v = Math.Min(v, this.Maximum);
			v = Math.Max(v, this.Minimum);
		}

		////if (this.TickFrequency != 0)
		////	v = Math.Round(v / this.TickFrequency) * this.TickFrequency;

		return v;
	}

	private void OnLostFocus(object sender, RoutedEventArgs e)
	{
		this.Text = this.DisplayValue.ToString("0.###");
		////this.Commit(false);
	}

	private void Commit(bool refocus)
	{
		try
		{
			this.DisplayValue = Convert.ToDouble(new DataTable().Compute(this.inputString, null));
			this.ErrorDisplay.Visibility = Visibility.Collapsed;
		}
		catch (Exception)
		{
			this.ErrorDisplay.Visibility = Visibility.Visible;
		}

		this.Text = this.DisplayValue.ToString("0.###");

		if (refocus)
		{
			this.InputBox.Focus();
			this.InputBox.CaretIndex = int.MaxValue;
		}
	}

	private async Task TickHeldKey()
	{
		while (this.keyHeld != Key.None)
		{
			await Application.Current.Dispatcher.InvokeAsync(() =>
			{
				this.TickKey(this.keyHeld);
			});

			await Task.Delay(10);
		}
	}

	private void TickKey(Key key)
	{
		if (key == Key.Up)
		{
			this.TickValue(true);
			this.Commit(true);
		}
		else if (key == Key.Down)
		{
			this.TickValue(false);
			this.Commit(true);
		}
	}

	private void TickValue(bool increase)
	{
		double delta = increase ? this.TickFrequency : -this.TickFrequency;

		if (Keyboard.IsKeyDown(Key.LeftShift))
			delta *= 10;

		if (Keyboard.IsKeyDown(Key.LeftCtrl))
			delta /= 10;

		double value = this.DisplayValue;
		double newValue = value + delta;
		newValue = this.Validate(newValue);

		if (newValue == value)
			return;

		this.bypassFocusLock = true;
		this.DisplayValue = newValue;
		this.bypassFocusLock = false;
	}

	private void OnDownClick(object sender, RoutedEventArgs e)
	{
		this.TickValue(false);
	}

	private void OnUpClick(object sender, RoutedEventArgs e)
	{
		this.TickValue(true);
	}

	private void OnSliderMouseMove(object sender, MouseEventArgs e)
	{
		if (this.Slider != SliderModes.Absolute)
			return;

		if (e.LeftButton == MouseButtonState.Pressed && this.Wrap)
		{
			WinPoint rightEdge = this.InputSlider.PointToScreen(new WinPoint(this.InputSlider.ActualWidth - 5, this.InputSlider.ActualHeight / 2));
			WinPoint leftEdge = this.InputSlider.PointToScreen(new WinPoint(6, this.InputSlider.ActualHeight / 2));

			if (WinCur.Position.X > rightEdge.X)
			{
				WinCur.Position = new DrawPoint((int)leftEdge.X, (int)leftEdge.Y);
			}

			if (WinCur.Position.X < leftEdge.X)
			{
				WinCur.Position = new DrawPoint((int)rightEdge.X, (int)rightEdge.Y);
			}
		}
	}

	private void OnWindowMouseDown(object? sender, MouseButtonEventArgs e)
	{
		FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);
		Keyboard.ClearFocus();
	}

	private void OnWindowDeactivated(object? sender, EventArgs e)
	{
		FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);
		Keyboard.ClearFocus();
	}

	private void OnSliderPreviewMouseDown(object? sender, MouseButtonEventArgs e)
	{
		this.relativeSliderStart = this.DisplayValue;
	}

	private void OnSliderPreviewMouseUp(object? sender, MouseButtonEventArgs e)
	{
		if (this.Slider == SliderModes.Relative)
		{
			this.relativeSliderStart = this.DisplayValue;
			this.SliderValue = 0;
		}
	}
}
