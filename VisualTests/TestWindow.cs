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
	}

	Shader basicShader;
	Shader colorShader;

	ColoredVertex[] shapeData = null!;
	uint[] shapeIndices = null!;
	GlHandle VAO;
	GlHandle VBO;
	GlHandle EBO;
	protected override void OnLoad () {
		base.OnLoad();

		shapeData = new ColoredVertex[] {
			new() { Position = new(  0.5f,  0.5f, 0.0f ), Color = new( 1, 0, 0 ) },
			new() { Position = new(  0.5f, -0.5f, 0.0f ), Color = new( 0, 1, 0 ) },
			new() { Position = new( -0.5f, -0.5f, 0.0f ), Color = new( 0, 0, 1 ) },
			new() { Position = new( -0.5f,  0.5f, 0.0f ), Color = new( 1, 0, 1 ) }
		};
		shapeIndices = new uint[] {
			0, 1, 3,
			1, 2, 3
		};

		VAO = GL.GenVertexArray();
		GL.BindVertexArray( VAO );

		VBO = GL.GenBuffer();
		GL.BindBuffer( BufferTarget.ArrayBuffer, VBO );
		ColoredVertex.Upload( shapeData, shapeData.Length );
		ColoredVertex.Link( position: colorShader.GetAttrib( "aPos" ), color: colorShader.GetAttrib( "aColor" ) );

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

		colorShader.Bind();
		GL.BindVertexArray( VAO );
		GL.DrawElements( PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero );

		SwapBuffers();
	}
}
