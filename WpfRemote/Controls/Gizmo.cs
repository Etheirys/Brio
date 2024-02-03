namespace WpfRemote.Controls;

using FFXIVClientStructs.Havok;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WpfRemote.DependencyProperties;
using WpfRemote.Meida3D;
using WpfRemote.Meida3D.Lines;

public class Gizmo : UserControl
{
	public static readonly IBind<hkQuaternionf> ValueDp = Binder.Register<hkQuaternionf, Gizmo>(nameof(Value), OnValueChanged);
	public static readonly IBind<hkQuaternionf?> RootRotationDp = Binder.Register<hkQuaternionf?, Gizmo>(nameof(RootRotation), OnRootRotationChanged, BindMode.OneWay);
	public static readonly IBind<double> TickDp = Binder.Register<double, Gizmo>(nameof(TickFrequency));

	public static readonly IBind<Quaternion> ValueQuatDp = Binder.Register<Quaternion, Gizmo>(nameof(ValueQuat), OnValueQuatChanged);
	public static readonly IBind<hkVector4f> EulerDp = Binder.Register<hkVector4f, Gizmo>(nameof(Euler), OnEulerChanged);

	private readonly QuaternionRotation3D? cameraRotation;
	private readonly RotationGizmo? rotationGizmo;
	private bool lockdp = false;

	private Quaternion worldSpaceDelta;
	private bool worldSpace;
	private bool isMouseDown = false;

	public Gizmo()
	{
		this.Background = new SolidColorBrush(Colors.Transparent);

		if (DesignerProperties.GetIsInDesignMode(this))
			return;

		this.MouseDown += this.OnMouseDown;
		this.MouseUp += this.OnMouseUp;
		this.MouseLeave += this.OnMouseLeave;
		this.MouseMove += this.OnMouseMove;
		this.MouseWheel += this.OnMouseWheel;

		Viewport3D viewport = new();
		this.AddChild(viewport);

		this.rotationGizmo = new RotationGizmo(this);
		viewport.Children.Add(this.rotationGizmo);

		viewport.Camera = new PerspectiveCamera(new Point3D(0, 0, -2.0), new Vector3D(0, 0, 1), new Vector3D(0, 1, 0), 35);

		this.cameraRotation = new();
		viewport.Camera.Transform = new RotateTransform3D(this.cameraRotation);
	}

	public double TickFrequency
	{
		get => TickDp.Get(this);
		set => TickDp.Set(this, value);
	}

	public hkQuaternionf Value
	{
		get => ValueDp.Get(this);
		set => ValueDp.Set(this, value);
	}

	public hkQuaternionf? RootRotation
	{
		get => RootRotationDp.Get(this);
		set => RootRotationDp.Set(this, value);
	}

	public Quaternion ValueQuat
	{
		get => ValueQuatDp.Get(this);
		set => ValueQuatDp.Set(this, value);
	}

	public hkVector4f Euler
	{
		get => EulerDp.Get(this);
		set => EulerDp.Set(this, value);
	}

	public Quaternion Root
	{
		get
		{
			if (this.RootRotation == null)
				return Quaternion.Identity;

			hkQuaternionf root = (hkQuaternionf)this.RootRotation;
			return new Quaternion(root.X, root.Y, root.Z, root.W);
		}
	}

	public bool WorldSpace
	{
		get
		{
			return this.worldSpace;
		}
		set
		{
			bool old = this.worldSpace;
			this.worldSpace = value;

			if (old && !value)
			{
				OnValueChanged(this, this.Value);
			}
			else
			{
				this.ValueQuat = Quaternion.Identity;

				if (this.rotationGizmo != null)
				{
					this.rotationGizmo.Transform = new RotateTransform3D(new QuaternionRotation3D(Quaternion.Identity));
				}
			}
		}
	}

	private static void OnValueChanged(Gizmo sender, hkQuaternionf value)
	{
		if (sender.isMouseDown)
			return;

		Quaternion valueQuat = new Quaternion(value.X, value.Y, value.Z, value.W);

		if (sender.RootRotation != null)
			valueQuat = sender.Root * valueQuat;

		sender.worldSpaceDelta = valueQuat;

		if (sender.WorldSpace)
			valueQuat = Quaternion.Identity;

		if (sender.rotationGizmo != null)
			sender.rotationGizmo.Transform = new RotateTransform3D(new QuaternionRotation3D(valueQuat));

		sender.ValueQuat = valueQuat;

		if (sender.lockdp)
			return;

		sender.lockdp = true;

		sender.Euler = sender.Value.ToEuler();

		sender.lockdp = false;
	}

	private static void OnRootRotationChanged(Gizmo sender, hkQuaternionf? value)
	{
		OnValueChanged(sender, sender.Value);
	}

	private static void OnValueQuatChanged(Gizmo sender, Quaternion value)
	{
		Quaternion newrot = value;

		if (sender.rotationGizmo != null)
			sender.rotationGizmo.Transform = new RotateTransform3D(new QuaternionRotation3D(newrot));

		if (sender.WorldSpace)
		{
			newrot *= sender.worldSpaceDelta;

			if (sender.rotationGizmo != null)
			{
				sender.rotationGizmo.Transform = new RotateTransform3D(new QuaternionRotation3D(Quaternion.Identity));
			}
		}

		if (sender.lockdp)
			return;

		sender.lockdp = true;

		if (sender.RootRotation != null)
		{
			Quaternion rootInv = sender.Root;
			rootInv.Invert();
			newrot = rootInv * newrot;
		}

		sender.Value = HkQuaternionExtensions.New((float)newrot.X, (float)newrot.Y, (float)newrot.Z, (float)newrot.W);

		sender.Euler = sender.Value.ToEuler();

		sender.lockdp = false;
	}

	private static void OnEulerChanged(Gizmo sender, hkVector4f val)
	{
		if (sender.lockdp)
			return;

		sender.lockdp = true;
		sender.Value = HkQuaternionExtensions.FromEuler(sender.Euler);
		sender.lockdp = false;
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		this.isMouseDown = true;
		Mouse.Capture(this);
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		this.isMouseDown = false;
		Mouse.Capture(null);

		if (e.ChangedButton == MouseButton.Right)
		{
			////this.LockedIndicator.IsChecked = this.rotationGizmo.LockHoveredGizmo();
			////this.LockedIndicator.IsEnabled = (bool)this.LockedIndicator.IsChecked;
			////this.LockedAxisDisplay.Text = GetAxisName(this.rotationGizmo.Locked?.Axis);
		}

		this.rotationGizmo?.Hover(null);
	}

	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		Point mousePosition = e.GetPosition(this);

		if (e.LeftButton != MouseButtonState.Pressed)
		{
			HitTestResult result = VisualTreeHelper.HitTest(this, mousePosition);
			this.rotationGizmo?.Hover(result?.VisualHit);
		}
		else
		{
			Point3D mousePos3D = new Point3D(mousePosition.X, mousePosition.Y, 0);
			this.rotationGizmo?.Drag(mousePos3D);
		}
	}

	private void OnMouseLeave(object sender, MouseEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
			return;

		this.rotationGizmo?.Hover(null);
	}

	private void OnMouseWheel(object sender, MouseWheelEventArgs e)
	{
		double delta = e.Delta > 0 ? this.TickFrequency : -this.TickFrequency;

		if (Keyboard.IsKeyDown(Key.LeftShift))
			delta *= 10;

		this.rotationGizmo?.Scroll(delta);
	}

	/*private unsafe void UpdateCamera()
	{
		if (this.cameraRotation == null)
			return;

		StudioCamera* pCamera = (StudioCamera*)DalamudServices.Camera->Camera;
		hkQuaternionf camRot = pCamera->Rotation;

		Quaternion q = this.cameraRotation.Quaternion;
		q.X = camRot.X;
		q.Y = camRot.Y;
		q.Z = camRot.Z;
		q.W = camRot.W;
		this.cameraRotation.Quaternion = q;
	}*/

	private class RotationGizmo : ModelVisual3D
	{
		private readonly Gizmo target;

		public RotationGizmo(Gizmo target)
		{
			this.target = target;

			Sphere sphere = new Sphere();
			sphere.Radius = 0.48;
			Color c = Colors.Black;
			c.A = 128;
			sphere.Material = new DiffuseMaterial(new SolidColorBrush(c));
			this.Children.Add(sphere);

			this.Children.Add(new AxisGizmo(Colors.Blue, new Vector3D(1, 0, 0)));
			this.Children.Add(new AxisGizmo(Colors.Green, new Vector3D(0, 1, 0)));
			this.Children.Add(new AxisGizmo(Colors.Red, new Vector3D(0, 0, 1)));
		}

		public AxisGizmo? Locked
		{
			get;
			private set;
		}

		public AxisGizmo? Hovered
		{
			get;
			private set;
		}

		public AxisGizmo? Active
		{
			get
			{
				if (this.Locked != null)
					return this.Locked;

				return this.Hovered;
			}
		}

		public bool LockHoveredGizmo()
		{
			if (this.Locked != null)
				this.Locked.Locked = false;

			this.Locked = this.Hovered;

			if (this.Locked != null)
				this.Locked.Locked = true;

			return this.Locked != null;
		}

		public void UnlockGizmo()
		{
			if (this.Locked != null)
				this.Locked.Locked = false;

			this.Locked = null;
		}

		public bool Hover(DependencyObject? visual)
		{
			if (this.Locked != null)
			{
				this.Hovered = null;
				return true;
			}

			AxisGizmo? gizmo = null;
			if (visual is Circle r)
			{
				gizmo = (AxisGizmo)VisualTreeHelper.GetParent(r);
			}
			else if (visual is Cylinder c)
			{
				gizmo = (AxisGizmo)VisualTreeHelper.GetParent(c);
			}

			if (this.Hovered != null)
				this.Hovered.Hovered = false;

			this.Hovered = gizmo;

			if (this.Hovered != null)
			{
				this.Hovered.Hovered = true;
				return true;
			}

			return false;
		}

		public void Drag(Point3D mousePosition)
		{
			if (this.Active == null)
				return;

			Vector3D angleDelta = this.Active.Drag(mousePosition);
			this.ApplyDelta(angleDelta);
		}

		public void Scroll(double delta)
		{
			if (this.Active == null)
				return;

			Vector3D angleDelta = this.Active.Axis * delta;
			this.ApplyDelta(angleDelta);
		}

		private void ApplyDelta(Vector3D angleEuler)
		{
			Quaternion angle = angleEuler.ToQuaternion();
			this.target.ValueQuat *= angle;
		}
	}

	private class AxisGizmo : ModelVisual3D
	{
		public readonly Vector3D Axis;
		private readonly Circle circle;
		private readonly Cylinder cylinder;
		private Color color;

		private Point3D? lastPoint;

		public AxisGizmo(Color color, Vector3D axis)
		{
			this.Axis = axis;
			this.color = color;

			Vector3D rotationAxis = new Vector3D(axis.Z, 0, axis.X);

			this.circle = new Circle();
			this.circle.Thickness = 1;
			this.circle.Color = color;
			this.circle.Radius = 0.5;
			this.circle.Transform = new RotateTransform3D(new AxisAngleRotation3D(axis, 90));
			this.Children.Add(this.circle);

			this.cylinder = new Cylinder();
			this.cylinder.Radius = 0.49;
			this.cylinder.Length = 0.20;
			this.cylinder.Transform = new RotateTransform3D(new AxisAngleRotation3D(axis, 90));
			this.cylinder.Material = new DiffuseMaterial(new SolidColorBrush(Colors.Transparent));
			this.Children.Add(this.cylinder);
		}

		public bool Hovered
		{
			set
			{
				if (!value)
				{
					this.circle.Color = this.color;
					this.circle.Thickness = 1;
					this.lastPoint = null;
				}
				else
				{
					this.circle.Color = Colors.Yellow;
					this.circle.Thickness = 3;
				}
			}
		}

		public bool Locked
		{
			set
			{
				if (!value)
				{
					this.circle.Color = this.color;
					this.circle.Thickness = 1;
					this.lastPoint = null;
				}
				else
				{
					this.circle.Color = Colors.White;
					this.circle.Thickness = 3;
				}
			}
		}

		public void StartDrag()
		{
			this.lastPoint = null;
		}

		public Vector3D Drag(Point3D mousePosition)
		{
			bool useCircularDrag = true;

			if (useCircularDrag)
			{
				Point3D? point = this.circle.NearestPoint2D(mousePosition);

				if (point == null)
					return default;

				point = this.circle.TransformToAncestor(this).Transform((Point3D)point);

				if (this.lastPoint == null)
				{
					this.lastPoint = point;
					return default;
				}
				else
				{
					Vector3D axis = new Vector3D(0, 1, 0);

					Vector3D from = (Vector3D)this.lastPoint;
					Vector3D to = (Vector3D)point;

					this.lastPoint = null;

					double angle = Vector3D.AngleBetween(from, to);

					Vector3D cross = Vector3D.CrossProduct(from, to);
					if (Vector3D.DotProduct(axis, cross) < 0)
						angle = -angle;

					// X rotation gizmo is always backwards...
					if (this.Axis.X >= 1)
						angle = -angle;

					float speed = 2;

					if (Keyboard.IsKeyDown(Key.LeftShift))
						speed = 4;

					if (Keyboard.IsKeyDown(Key.RightShift))
						speed = 4;

					if (Keyboard.IsKeyDown(Key.LeftCtrl))
						speed = 0.5f;

					if (Keyboard.IsKeyDown(Key.RightCtrl))
						speed = 0.5f;

					return this.Axis * (angle * speed);
				}
			}
			else
			{
				if (this.lastPoint == null)
				{
					this.lastPoint = mousePosition;
					return default;
				}
				else
				{
					Vector3D delta = (Point3D)this.lastPoint - mousePosition;
					this.lastPoint = mousePosition;

					float speed = 0.5f;

					if (Keyboard.IsKeyDown(Key.LeftShift))
						speed = 2;

					if (Keyboard.IsKeyDown(Key.LeftCtrl))
						speed = 0.25f;

					double distPos = Math.Max(delta.X, delta.Y);
					double distNeg = Math.Min(delta.X, delta.Y);

					double dist = distNeg;
					if (Math.Abs(distPos) > Math.Abs(distNeg))
						dist = distPos;

					return this.Axis * (-dist * speed);
				}
			}
		}
	}
}
