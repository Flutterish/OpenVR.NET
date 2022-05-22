using System.Numerics;
using Valve.VR;

namespace OpenVR.NET;

public static class Extensions {
	public static Vector3 ExtractPosition ( ref HmdMatrix34_t mat ) {
		return new Vector3( mat.m3, mat.m7, -mat.m11 );
	}

	public static Quaternion ExtractRotation ( ref HmdMatrix34_t mat ) {
		Quaternion q = default;
		q.W = MathF.Sqrt( MathF.Max( 0, 1 + mat.m0 + mat.m5 + mat.m10 ) ) / 2;
		q.X = MathF.Sqrt( MathF.Max( 0, 1 + mat.m0 - mat.m5 - mat.m10 ) ) / 2;
		q.Y = MathF.Sqrt( MathF.Max( 0, 1 - mat.m0 + mat.m5 - mat.m10 ) ) / 2;
		q.Z = MathF.Sqrt( MathF.Max( 0, 1 - mat.m0 - mat.m5 + mat.m10 ) ) / 2;
		q.X = MathF.CopySign( q.X, mat.m9 - mat.m6 );
		q.Y = MathF.CopySign( q.Y, mat.m2 - mat.m8 );
		q.Z = MathF.CopySign( q.Z, mat.m1 - mat.m4 );

		var scale = 1 / q.LengthSquared();
		return new Quaternion( q.X * -scale, q.Y * -scale, q.Z * -scale, q.W * scale );
	}
}
