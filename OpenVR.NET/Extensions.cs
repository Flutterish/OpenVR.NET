using System;
using System.Numerics;
using Valve.VR;

namespace OpenVR.NET {
	public static class Extensions {
		public static Vector3 ExtractPosition ( this HmdMatrix34_t mat ) {
			return new Vector3( mat.m3, mat.m7, -mat.m11 );
		}

		public static Quaternion ExtractRotation ( this HmdMatrix34_t mat ) {
			static float CopySign ( float a, float b ) {
				if ( MathF.Sign( a ) != MathF.Sign( b ) )
					return -a;
				else return a;
			}

			Quaternion q = default;
			q.W = MathF.Sqrt( MathF.Max( 0, 1 + mat.m0 + mat.m5 + mat.m10 ) ) / 2;
			q.X = MathF.Sqrt( MathF.Max( 0, 1 + mat.m0 - mat.m5 - mat.m10 ) ) / 2;
			q.Y = MathF.Sqrt( MathF.Max( 0, 1 - mat.m0 + mat.m5 - mat.m10 ) ) / 2;
			q.Z = MathF.Sqrt( MathF.Max( 0, 1 - mat.m0 - mat.m5 + mat.m10 ) ) / 2;
			q.X = CopySign( q.X, mat.m9 - mat.m6 );
			q.Y = CopySign( q.Y, mat.m2 - mat.m8 );
			q.Z = CopySign( q.Z, mat.m1 - mat.m4 );

			var scale = 1 / q.LengthSquared();
			return new Quaternion( q.X * -scale, q.Y * -scale, q.Z * -scale, q.W * scale );
		}
	}
}
