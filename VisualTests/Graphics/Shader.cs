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

	public int GetAttrib ( string name )
		=> GL.GetAttribLocation( Handle, name );

	Dictionary<string, int> uniforms = new();
	int uniformLocation ( string name ) {
		if ( !uniforms.TryGetValue( name, out var loc ) )
			uniforms.Add( name, loc = GL.GetUniformLocation( Handle, name ) );

		return loc;
	}

	public void SetUniform ( string name, Color4 value ) {
		GL.Uniform4( uniformLocation( name ), value );
	}
}
