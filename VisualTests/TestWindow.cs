using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using VisualTests.Graphics;
using VisualTests.Vetrices;

namespace VisualTests;
internal class TestWindow : GameWindow {
	public TestWindow ( GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings ) 
		: base( gameWindowSettings, nativeWindowSettings ) {

		basicShader = new( "Resources/Shaders/basic.vert", "Resources/Shaders/basic.frag" );
	}

	Shader basicShader;

	Vector3[] triangleData = null!;
	GlHandle VBO;
	GlHandle VAO;
	protected override void OnLoad () {
		base.OnLoad();

		triangleData = new Vector3[] {
			new( -0.5f, -0.5f, 0.0f ),
			new(  0.5f, -0.5f, 0.0f ),
			new(  0.0f,  0.5f, 0.0f )
		};
		VBO = GL.GenBuffer();
		GL.BindBuffer( BufferTarget.ArrayBuffer, VBO );
		PositionVertex.Upload( triangleData, triangleData.Length );

		VAO = GL.GenVertexArray();
		GL.BindVertexArray( VAO );
		PositionVertex.Link( position: 0 );
	}

	float time;
	protected override void OnRenderFrame ( FrameEventArgs args ) {
		base.OnRenderFrame( args );
		var deltaTime = (float)args.Time;
		time += deltaTime;

		GL.Viewport( 0, 0, Size.X, Size.Y );
		GL.ClearColor( 0.2f, 0.3f, 0.3f, 1.0f );
		GL.Clear( ClearBufferMask.ColorBufferBit );

		basicShader.Bind();
		GL.BindVertexArray( VAO );
		GL.DrawArrays( PrimitiveType.Triangles, 0, 3 );

		SwapBuffers();
	}
}
