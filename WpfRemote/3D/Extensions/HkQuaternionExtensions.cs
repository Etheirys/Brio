namespace FFXIVClientStructs.Havok;

using System;
using System.Numerics;

public static class HkQuaternionExtensions
{
	private static readonly float Deg2Rad = ((float)Math.PI * 2) / 360;
	private static readonly float Rad2Deg = 360 / ((float)Math.PI * 2);

	public static hkQuaternionf New(float x, float y, float z, float w)
	{
		hkQuaternionf v = default;
		v.X = x;
		v.Y = y;
		v.Z = z;
		v.W = w;
		return v;
	}

	public static Quaternion ToQuaternion(this hkQuaternionf q) => new Quaternion(q.X, q.Y, q.Z, q.W);
	public static hkQuaternionf ToHavok(this Quaternion q) => new hkQuaternionf
	{
		X = q.X,
		Y = q.Y,
		Z = q.Z,
		W = q.W,
	};

	public static hkQuaternionf FromQuaternion(this hkQuaternionf tar, Quaternion q)
	{
		tar.X = q.X;
		tar.Y = q.Y;
		tar.Z = q.Z;
		tar.W = q.W;
		return tar;
	}

	public static hkQuaternionf FromEuler(hkVector4f euler)
	{
		float yaw = euler.Y * Deg2Rad;
		float pitch = euler.X * Deg2Rad;
		float roll = euler.Z * Deg2Rad;

		float c1 = MathF.Cos(yaw / 2);
		float s1 = MathF.Sin(yaw / 2);
		float c2 = MathF.Cos(pitch / 2);
		float s2 = MathF.Sin(pitch / 2);
		float c3 = MathF.Cos(roll / 2);
		float s3 = MathF.Sin(roll / 2);

		float c1c2 = c1 * c2;
		float s1s2 = s1 * s2;

		hkQuaternionf v = default;
		v.X = (c1c2 * s3) + (s1s2 * c3);
		v.Y = (s1 * c2 * c3) + (c1 * s2 * s3);
		v.Z = (c1 * s2 * c3) - (s1 * c2 * s3);
		v.W = (c1c2 * c3) - (s1s2 * s3);
		return v;
	}

	public static hkVector4f ToEuler(this hkQuaternionf self)
	{
		hkVector4f v = default;

		double test = (self.X * self.Y) + (self.Z * self.W);

		if (test > 0.4995f)
		{
			v.Y = 2f * (float)Math.Atan2(self.X, self.Y);
			v.X = (float)Math.PI / 2;
			v.Z = 0;
		}
		else if (test < -0.4995f)
		{
			v.Y = -2f * (float)Math.Atan2(self.X, self.W);
			v.X = -(float)Math.PI / 2;
			v.Z = 0;
		}
		else
		{
			double sqx = self.X * self.X;
			double sqy = self.Y * self.Y;
			double sqz = self.Z * self.Z;

			v.Y = (float)Math.Atan2((2 * self.Y * self.W) - (2 * self.X * self.Z), 1 - (2 * sqy) - (2 * sqz));
			v.X = (float)Math.Asin(2 * test);
			v.Z = (float)Math.Atan2((2 * self.X * self.W) - (2 * self.Y * self.Z), 1 - (2 * sqx) - (2 * sqz));
		}

		v.X = NormalizeAngle(v.X * Rad2Deg);
		v.Y = NormalizeAngle(v.Y * Rad2Deg);
		v.Z = NormalizeAngle(v.Z * Rad2Deg);

		return v;
	}

	private static float NormalizeAngle(float angle)
	{
		while (angle > 360)
			angle -= 360;

		while (angle < 0)
			angle += 360;

		return angle;
	}
}
