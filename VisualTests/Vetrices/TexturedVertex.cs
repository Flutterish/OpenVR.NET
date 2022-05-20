namespace VisualTests.Vetrices;

public struct TexturedVertex {
	public Vector3 Position;
	public Vector2 UV;

	public static readonly int Stride = sizeof( float ) * 5;
	public static void Upload ( TexturedVertex[] data, int length, BufferUsageHint usage = BufferUsageHint.StaticDraw ) {
		GL.BufferData( BufferTarget.ArrayBuffer, length * Stride, data, usage );
	}

	public static void Link ( int position = 0, int uv = 1 ) {
		GL.VertexAttribPointer( position, 3, VertexAttribPointerType.Float, false, Stride, 0 );
		GL.EnableVertexAttribArray( position );
		GL.VertexAttribPointer( uv, 2, VertexAttribPointerType.Float, false, Stride, 3 * sizeof( float ) );
		GL.EnableVertexAttribArray( uv );
	}
}
