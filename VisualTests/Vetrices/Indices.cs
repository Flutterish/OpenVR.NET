namespace VisualTests.Vetrices;

public static class Indices {
	public static void Upload ( uint[] indices, int length, BufferUsageHint usage = BufferUsageHint.StaticDraw ) {
		GL.BufferData( BufferTarget.ElementArrayBuffer, length * sizeof( uint ), indices, usage );
	}
}
