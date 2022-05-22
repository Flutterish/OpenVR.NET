namespace VisualTests;

public class Transform {
	public Transform? Parent;
	public Vector3 Position;
	public Vector3 Scale = Vector3.One;
	public Quaternion Rotation = Quaternion.Identity;

	public Vector3 Forward => Rotation * Vector3.UnitZ;
	public Vector3 Right => Rotation * Vector3.UnitX;
	public Vector3 Up => Rotation * Vector3.UnitY;

	public Matrix4 LocalMatrix => Matrix4.CreateFromQuaternion( Rotation )
		* Matrix4.CreateScale( Scale )
		* Matrix4.CreateTranslation( Position );

	public Matrix4 Matrix => Parent is null
		? LocalMatrix
		: ( LocalMatrix * Parent.Matrix );

	public Matrix4 LocalMatrixInverse => Matrix4.CreateTranslation( -Position )
		* Matrix4.CreateScale( 1 / Scale.X, 1 / Scale.Y, 1 / Scale.Z )
		* Matrix4.CreateFromQuaternion( Rotation.Inverted() );

	public Matrix4 MatrixInverse => Parent is null
		? LocalMatrixInverse
		: ( Parent.MatrixInverse * LocalMatrixInverse );
}
