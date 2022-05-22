namespace VisualTests.Vertices;

public static class PositionVertex {
	public static readonly int Stride = sizeof( float ) * 3;
	public static void Upload ( Vector3[] data, int length, BufferUsageHint usage = BufferUsageHint.StaticDraw ) {
		GL.BufferData( BufferTarget.ArrayBuffer, length * Stride, data, usage );
	}

	public static void Link ( int position = 0 ) {
		GL.VertexAttribPointer( position, 3, VertexAttribPointerType.Float, false, Stride, 0 );
		GL.EnableVertexAttribArray( position );
	}
}
