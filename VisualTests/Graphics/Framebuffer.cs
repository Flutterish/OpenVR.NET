namespace VisualTests.Graphics;

public class Framebuffer {
	public readonly GlHandle Handle;
	public readonly GlHandle DepthHandle;
	public readonly Texture Texture;
	public int Width { get; private set; }
	public int Height { get; private set; }

	public Framebuffer () {
		Handle = GL.GenFramebuffer();
		GL.BindFramebuffer( FramebufferTarget.Framebuffer, Handle );

		Texture = new();
		GL.BindTexture( TextureTarget.Texture2D, Texture.Handle );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear );
		GL.FramebufferTexture2D( FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, Texture.Handle, 0 );

		DepthHandle = GL.GenRenderbuffer();
		GL.BindRenderbuffer( RenderbufferTarget.Renderbuffer, DepthHandle );
		GL.FramebufferRenderbuffer( FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, DepthHandle );

		GL.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
	}

	public void Resize ( int width, int height ) {
		if ( width == Width && height == Height )
			return;

		Texture.Resize( width, height );
		GL.BindRenderbuffer( RenderbufferTarget.Renderbuffer, DepthHandle );
		GL.RenderbufferStorage( RenderbufferTarget.Renderbuffer, RenderbufferInternalFormat.DepthComponent24, width, height );

		Width = width;
		Height = height;
	}

	public void Bind () {
		GL.BindFramebuffer( FramebufferTarget.Framebuffer, Handle );
	}

	public void Unbind () {
		GL.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
	}
}
