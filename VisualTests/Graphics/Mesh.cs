namespace VisualTests.Graphics;

public class Mesh<Tvertex> {
	public readonly List<Tvertex> Vertices = new();
	public readonly List<uint> Indices = new();

	Action<Tvertex[]> uploadVertices;
	GlHandle VAO;
	GlHandle VBO;
	GlHandle EBO;
	public Mesh ( Action<Tvertex[]> uploadVertices ) {
		this.uploadVertices = uploadVertices;
	}

	void ensureValid () {
		if ( VAO == 0 ) {
			VAO = GL.GenVertexArray();
			VBO = GL.GenBuffer();
			EBO = GL.GenBuffer();
		}
	}

	public void Link ( Shader shader, Action<Shader> linkVertex ) {
		ensureValid();
		GL.BindVertexArray( VAO );
		GL.BindBuffer( BufferTarget.ArrayBuffer, VBO );
		GL.BindBuffer( BufferTarget.ElementArrayBuffer, EBO );

		linkVertex( shader );
	}

	public void Upload () {
		ensureValid();
		GL.BindVertexArray( VAO );
		GL.BindBuffer( BufferTarget.ArrayBuffer, VBO );
		GL.BindBuffer( BufferTarget.ElementArrayBuffer, EBO );

		uploadVertices( Vertices.ToArray() );
		VisualTests.Vertices.Indices.Upload( Indices.ToArray(), Indices.Count );
	}

	public void Bind () {
		ensureValid();
		GL.BindVertexArray( VAO );
	}
}
