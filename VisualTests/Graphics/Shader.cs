namespace VisualTests.Graphics;

public class Shader {
	public readonly GlHandle Handle;

	public Shader ( string vertPath, string fragPath ) {
		var vertSource = File.ReadAllText( vertPath );
		var fragSource = File.ReadAllText( fragPath );

		GlHandle vert = GL.CreateShader( ShaderType.VertexShader );
		GlHandle frag = GL.CreateShader( ShaderType.FragmentShader );

		GL.ShaderSource( vert, vertSource );
		GL.ShaderSource( frag, fragSource );

		Handle = GL.CreateProgram();
		GL.AttachShader( Handle, vert );
		GL.AttachShader( Handle, frag );
		GL.LinkProgram( Handle );

		GL.DeleteShader( vert );
		GL.DeleteShader( frag );
	}

	public void Bind () {
		GL.UseProgram( Handle );
	}
}
