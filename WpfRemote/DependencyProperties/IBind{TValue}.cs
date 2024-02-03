namespace WpfUtils.DependencyProperties;

using System.Windows;

public interface IBind<TValue>
{
	TValue Get(DependencyObject control);
	void Set(DependencyObject control, TValue value);
}
