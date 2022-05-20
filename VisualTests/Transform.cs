namespace VisualTests;

public class Transform {
	public Transform? Parent;
	public Vector3 Position;
	public Vector3 Scale = Vector3.One;
	public Quaternion Rotation = Quaternion.Identity;

	public Matrix4 LocalMatrix => Matrix4.CreateFromQuaternion( Rotation )
		* Matrix4.CreateScale( Scale )
		* Matrix4.CreateTranslation( Position );

	public Matrix4 Matrix => Parent is null
		? LocalMatrix
		: ( LocalMatrix * Parent.Matrix );
}
