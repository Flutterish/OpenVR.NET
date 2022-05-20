using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
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

		framebuffer = new();
	}

	Shader basicShader;
	Shader colorShader;
	Shader textureShader;
	Shader unlitShader;
	Texture susie;

	Framebuffer framebuffer;

	Transform shapeTransform = new();
	Transform cameraTransform = new();

	TexturedVertex[] shapeData = null!;
	GlHandle VAO;
	GlHandle VBO;

	TexturedVertex[] blitData = null!;
	uint[] blitIndices = null!;
	GlHandle blitVAO;
	GlHandle blitVBO;
	GlHandle blitEBO;
	protected override void OnLoad () {
		base.OnLoad();

		shapeData = new TexturedVertex[] {
			new() { Position = new( -0.5f, -0.5f, -0.5f ), UV = new( 0.0f, 0.0f ) },
			new() { Position = new(  0.5f, -0.5f, -0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new(  0.5f,  0.5f, -0.5f ), UV = new( 1.0f, 1.0f ) },
			new() { Position = new(  0.5f,  0.5f, -0.5f ), UV = new( 1.0f, 1.0f ) },
			new() { Position = new( -0.5f,  0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new( -0.5f, -0.5f, -0.5f ), UV = new( 0.0f, 0.0f ) },
			new() { Position = new( -0.5f, -0.5f,  0.5f ), UV = new( 0.0f, 0.0f ) },
			new() { Position = new(  0.5f, -0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new(  0.5f,  0.5f,  0.5f ), UV = new( 1.0f, 1.0f ) },
			new() { Position = new(  0.5f,  0.5f,  0.5f ), UV = new( 1.0f, 1.0f ) },
			new() { Position = new( -0.5f,  0.5f,  0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new( -0.5f, -0.5f,  0.5f ), UV = new( 0.0f, 0.0f ) },
			new() { Position = new( -0.5f,  0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new( -0.5f,  0.5f, -0.5f ), UV = new( 1.0f, 1.0f ) },
			new() { Position = new( -0.5f, -0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new( -0.5f, -0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new( -0.5f, -0.5f,  0.5f ), UV = new( 0.0f, 0.0f ) },
			new() { Position = new( -0.5f,  0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new(  0.5f,  0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new(  0.5f,  0.5f, -0.5f ), UV = new( 1.0f, 1.0f ) },
			new() { Position = new(  0.5f, -0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new(  0.5f, -0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new(  0.5f, -0.5f,  0.5f ), UV = new( 0.0f, 0.0f ) },
			new() { Position = new(  0.5f,  0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new( -0.5f, -0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new(  0.5f, -0.5f, -0.5f ), UV = new( 1.0f, 1.0f ) },
			new() { Position = new(  0.5f, -0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new(  0.5f, -0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new( -0.5f, -0.5f,  0.5f ), UV = new( 0.0f, 0.0f ) },
			new() { Position = new( -0.5f, -0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new( -0.5f,  0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) },
			new() { Position = new(  0.5f,  0.5f, -0.5f ), UV = new( 1.0f, 1.0f ) },
			new() { Position = new(  0.5f,  0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new(  0.5f,  0.5f,  0.5f ), UV = new( 1.0f, 0.0f ) },
			new() { Position = new( -0.5f,  0.5f,  0.5f ), UV = new( 0.0f, 0.0f ) },
			new() { Position = new( -0.5f,  0.5f, -0.5f ), UV = new( 0.0f, 1.0f ) }
		};

		VAO = GL.GenVertexArray();
		GL.BindVertexArray( VAO );

		VBO = GL.GenBuffer();
		GL.BindBuffer( BufferTarget.ArrayBuffer, VBO );
		TexturedVertex.Upload( shapeData, shapeData.Length );
		TexturedVertex.Link( position: unlitShader.GetAttrib( "aPos" ), uv: unlitShader.GetAttrib( "aUv" ) );

		blitData = new TexturedVertex[] {
			new() { Position = new(  1,  1, 0 ), UV = new( 1, 1 ) },
			new() { Position = new(  1, -1, 0 ), UV = new( 1, 0 ) },
			new() { Position = new( -1, -1, 0 ), UV = new( 0, 0 ) },
			new() { Position = new( -1,  1, 0 ), UV = new( 0, 1 ) }
		};
		blitIndices = new uint[] {
			0, 1, 3,
			1, 2, 3
		};

		blitVAO = GL.GenVertexArray();
		GL.BindVertexArray( blitVAO );

		blitVBO = GL.GenBuffer();
		GL.BindBuffer( BufferTarget.ArrayBuffer, blitVBO );
		TexturedVertex.Upload( blitData, blitData.Length );
		TexturedVertex.Link( position: textureShader.GetAttrib( "aPos" ), uv: textureShader.GetAttrib( "aUv" ) );

		blitEBO = GL.GenBuffer();
		GL.BindBuffer( BufferTarget.ElementArrayBuffer, blitEBO );
		Indices.Upload( blitIndices, blitIndices.Length );
	}

	protected override void OnRenderFrame ( FrameEventArgs args ) {
		base.OnRenderFrame( args );

		framebuffer.Resize( Size.X, Size.Y );
		framebuffer.Bind();
		GL.Viewport( 0, 0, framebuffer.Width, framebuffer.Height );
		GL.ClearColor( 0.2f, 0.3f, 0.3f, 1.0f );
		GL.Enable( EnableCap.DepthTest );
		GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );
		unlitShader.Bind();
		susie.Bind();
		unlitShader.SetUniform( "gProj", cameraTransform.MatrixInverse * Matrix4.CreateScale( 1, 1, -1 ) * Matrix4.CreatePerspectiveFieldOfView(
			MathF.PI / 2,
			(float)Size.X / Size.Y,
			0.01f,
			1000f
		) );
		GL.BindVertexArray( VAO );
		var cubePositions = new Vector3[] {
			new( 2.0f,  5.0f, 15.0f),
			new(-1.5f, -2.2f, 2.5f),
			new(-3.8f, -2.0f, 12.3f),
			new( 2.4f, -0.4f, 3.5f),
			new(-1.7f,  3.0f, 7.5f),
			new( 1.3f, -2.0f, 2.5f),
			new( 1.5f,  2.0f, 2.5f),
			new( 1.5f,  0.2f, 1.5f),
			new(-1.3f,  1.0f, 1.5f)
		};
		foreach ( var pos in cubePositions ) {
			shapeTransform.Position = pos;
			unlitShader.SetUniform( "transform", shapeTransform.Matrix );
			GL.DrawArrays( PrimitiveType.Triangles, 0, 36 );
		}
		framebuffer.Unbind();

		GL.Viewport( 0, 0, Size.X, Size.Y );
		GL.Disable( EnableCap.DepthTest );
		textureShader.Bind();
		framebuffer.Texture.Bind();
		GL.BindVertexArray( blitVAO );
		GL.DrawElements( PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero );

		SwapBuffers();
	}

	protected override void OnUpdateFrame ( FrameEventArgs args ) {
		base.OnUpdateFrame( args );

		shapeTransform.Position = new( 0, 0, 1.6f );
		shapeTransform.Rotation *= Quaternion.FromAxisAngle( new Vector3( 1, 0.3f, MathF.Sin( (float)args.Time ) + 1 ).Normalized(), (float)args.Time );

		var eulerX = Math.Clamp( MouseState.Y / Size.Y * 180 - 90, -89, 89 );
		var eulerY = MouseState.X / Size.X * 720 + 360;

		cameraTransform.Rotation = Quaternion.FromAxisAngle( Vector3.UnitY, eulerY * MathF.PI / 180 )
			* Quaternion.FromAxisAngle( Vector3.UnitX, eulerX * MathF.PI / 180 );

		Vector3 dir = Vector3.Zero;
		if ( KeyboardState.IsKeyDown( Keys.D ) )
			dir += cameraTransform.Right;
		if ( KeyboardState.IsKeyDown( Keys.A ) )
			dir -= cameraTransform.Right;
		if ( KeyboardState.IsKeyDown( Keys.W ) )
			dir += cameraTransform.Forward;
		if ( KeyboardState.IsKeyDown( Keys.S ) )
			dir -= cameraTransform.Forward;
		if ( KeyboardState.IsKeyDown( Keys.Space ) )
			dir += cameraTransform.Up;
		if ( KeyboardState.IsKeyDown( Keys.LeftControl ) )
			dir -= cameraTransform.Up;

		if ( dir != Vector3.Zero )
			cameraTransform.Position += dir.Normalized() * (float)args.Time;
	}
}
