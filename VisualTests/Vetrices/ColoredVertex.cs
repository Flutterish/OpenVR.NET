namespace VisualTests.Vetrices;

public struct ColoredVertex {
	public Vector3 Position;
	public Vector3 Color;

	public static readonly int Stride = sizeof( float ) * 6;
	public static void Upload ( ColoredVertex[] data, int length, BufferUsageHint usage = BufferUsageHint.StaticDraw ) {
		GL.BufferData( BufferTarget.ArrayBuffer, length * Stride, data, usage );
	}

	public static void Link ( int position = 0, int color = 1 ) {
		GL.VertexAttribPointer( position, 3, VertexAttribPointerType.Float, false, Stride, 0 );
		GL.EnableVertexAttribArray( position );
		GL.VertexAttribPointer( color, 3, VertexAttribPointerType.Float, false, Stride, 3 * sizeof( float ) );
		GL.EnableVertexAttribArray( color );
	}
}
