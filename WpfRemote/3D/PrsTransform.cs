namespace WpfUtils.Meida3D;

using System.Windows.Media.Media3D;

public class PrsTransform
{
	private readonly Transform3DGroup transform = new Transform3DGroup();
	private readonly TranslateTransform3D position = new TranslateTransform3D();
	private readonly QuaternionRotation3D rotation = new QuaternionRotation3D();
	private readonly ScaleTransform3D scale = new ScaleTransform3D();

	public PrsTransform()
	{
		this.transform.Children.Add(this.position);

		RotateTransform3D rotation = new RotateTransform3D();
		rotation.Rotation = this.rotation;
		this.transform.Children.Add(rotation);

		this.transform.Children.Add(this.scale);
	}

	public Transform3DGroup Transform => this.transform;
	public bool IsAffine => this.transform.IsAffine;
	public Matrix3D Value => this.transform.Value;

	public Vector3D Scale3D
	{
		get
		{
			return new Vector3D(this.scale.ScaleX, this.scale.ScaleY, this.scale.ScaleZ);
		}

		set
		{
			this.scale.ScaleX = value.X;
			this.scale.ScaleY = value.Y;
			this.scale.ScaleZ = value.Z;
		}
	}

	public double UniformScale
	{
		get
		{
			double scale = this.scale.ScaleX;
			this.UniformScale = scale;
			return scale;
		}

		set
		{
			this.scale.ScaleX = value;
			this.scale.ScaleY = value;
			this.scale.ScaleZ = value;
		}
	}

	public Quaternion Rotation
	{
		get => this.rotation.Quaternion;
		set => this.rotation.Quaternion = value;
	}

	public Vector3D Position
	{
		get
		{
			return new Vector3D(this.position.OffsetX, this.position.OffsetY, this.position.OffsetZ);
		}

		set
		{
			this.position.OffsetX = value.X;
			this.position.OffsetY = value.Y;
			this.position.OffsetZ = value.Z;

			/*this.scale.CenterX = value.X;
			this.scale.CenterY = value.Y;
			this.scale.CenterZ = value.Z;*/
		}
	}

	public void Reset()
	{
		this.Position = new Vector3D(0, 0, 0);
		this.Rotation = Quaternion.Identity;
		this.UniformScale = 1;
	}
}
