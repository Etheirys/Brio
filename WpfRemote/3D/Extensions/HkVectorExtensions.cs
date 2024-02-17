namespace FFXIVClientStructs.Havok;

using System.Numerics;

public static class HkVectorExtensions
{
	public static Vector3 ToVector3(this hkVector4f vec) => new Vector3(vec.X, vec.Y, vec.Z);
	public static Vector4 ToVector4(this hkVector4f vec) => new Vector4(vec.X, vec.Y, vec.Z, vec.W);

	public static hkVector4f ToHavok(this Vector3 v) => new hkVector4f { X = v.X, Y = v.Y, Z = v.Z, W = 1 };

	public static hkVector4f SetFromVector3(this hkVector4f tar, Vector3 vec)
	{
		tar.X = vec.X;
		tar.Y = vec.Y;
		tar.Z = vec.Z;
		return tar;
	}
}
