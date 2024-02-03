namespace WpfUtils;

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

public static class Dispatch
{
	////public static SwitchToUiAwaitable MainThread() => new();
	public static SwitchFromUiAwaitable NonUiThread() => new();

	public static SwitchToMainThreadAwaitable MainThread(this DispatcherObject self) => new(self.Dispatcher);
	public static SwitchToMainThreadAwaitable MainThread(this Dispatcher self) => new(self);

	public struct SwitchToMainThreadAwaitable : INotifyCompletion
	{
		private readonly Dispatcher dispatch;

		public SwitchToMainThreadAwaitable(Dispatcher dispatcher)
		{
			this.dispatch = dispatcher;
		}

		public bool IsCompleted => this.dispatch?.CheckAccess() == true;

		public SwitchToMainThreadAwaitable GetAwaiter() => this;
		public void GetResult()
		{
		}

		public void OnCompleted(Action continuation)
		{
			this.dispatch.BeginInvoke(continuation);
		}
	}

	public struct SwitchFromUiAwaitable : INotifyCompletion
	{
		private readonly Dispatcher? dispatch;

		public SwitchFromUiAwaitable()
		{
			this.dispatch = Dispatcher.FromThread(Thread.CurrentThread);

			if (this.dispatch == null)
			{
				this.dispatch = Application.Current?.Dispatcher;
			}
		}

		public bool IsCompleted => this.dispatch?.CheckAccess() == false;

		public SwitchFromUiAwaitable GetAwaiter()
		{
			return this;
		}

		public void GetResult()
		{
		}

		public void OnCompleted(Action continuation)
		{
			Task.Run(continuation);
		}
	}
}
