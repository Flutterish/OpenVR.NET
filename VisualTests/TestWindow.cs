using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using VisualTests.Graphics;
using VisualTests.Vetrices;

namespace VisualTests;
internal class TestWindow : GameWindow {
	public TestWindow ( GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings ) 
		: base( gameWindowSettings, nativeWindowSettings ) {

		basicShader = new( "Resources/Shaders/basic.vert", "Resources/Shaders/basic.frag" );
		colorShader = new( "Resources/Shaders/colored.vert", "Resources/Shaders/colored.frag" );
		textureShader = new( "Resources/Shaders/textured.vert", "Resources/Shaders/textured.frag" );
		unlitShader = new( "Resources/Shaders/unlit.vert", "Resources/Shaders/unlit.frag" );
		susie = new();
		susie.Upload( "Resources/Textures/susie.png" );
	}

	Shader basicShader;
	Shader colorShader;
	Shader textureShader;
	Shader unlitShader;
	Texture susie;

	Transform shapeTransform = new();

	TexturedVertex[] shapeData = null!;
	uint[] shapeIndices = null!;
	GlHandle VAO;
	GlHandle VBO;
	GlHandle EBO;
	protected override void OnLoad () {
		base.OnLoad();

		shapeData = new TexturedVertex[] {
			new() { Position = new(  0.5f,  0.5f, 0.0f ), UV = new( 1, 1 ) },
			new() { Position = new(  0.5f, -0.5f, 0.0f ), UV = new( 1, 0 ) },
			new() { Position = new( -0.5f, -0.5f, 0.0f ), UV = new( 0, 0 ) },
			new() { Position = new( -0.5f,  0.5f, 0.0f ), UV = new( 0, 1 ) }
		};
		shapeIndices = new uint[] {
			0, 1, 3,
			1, 2, 3
		};

		VAO = GL.GenVertexArray();
		GL.BindVertexArray( VAO );

		VBO = GL.GenBuffer();
		GL.BindBuffer( BufferTarget.ArrayBuffer, VBO );
		TexturedVertex.Upload( shapeData, shapeData.Length );
		TexturedVertex.Link( position: unlitShader.GetAttrib( "aPos" ), uv: unlitShader.GetAttrib( "aUv" ) );

		EBO = GL.GenBuffer();
		GL.BindBuffer( BufferTarget.ElementArrayBuffer, EBO );
		Indices.Upload( shapeIndices, shapeIndices.Length );
	}

	float time;
	protected override void OnRenderFrame ( FrameEventArgs args ) {
		base.OnRenderFrame( args );
		var deltaTime = (float)args.Time;
		time += deltaTime;

		GL.Viewport( 0, 0, Size.X, Size.Y );
		GL.ClearColor( 0.2f, 0.3f, 0.3f, 1.0f );
		GL.Clear( ClearBufferMask.ColorBufferBit );

		unlitShader.Bind();
		susie.Bind();
		unlitShader.SetUniform( "transform", shapeTransform.Matrix );
		unlitShader.SetUniform( "gProj", Matrix4.CreateScale( 1, 1, -1 ) * Matrix4.CreatePerspectiveFieldOfView(
			MathF.PI / 2,
			(float)Size.X / Size.Y,
			0.01f,
			1000f
		) );
		GL.BindVertexArray( VAO );
		GL.DrawElements( PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero );

		SwapBuffers();
	}

	protected override void OnUpdateFrame ( FrameEventArgs args ) {
		base.OnUpdateFrame( args );

		shapeTransform.Position = new( 0, 0, 1 );
		shapeTransform.Rotation *= Quaternion.FromAxisAngle( new Vector3( 1, 0.3f, MathF.Sin( (float)args.Time ) + 1 ).Normalized(), (float)args.Time );
	}
}
