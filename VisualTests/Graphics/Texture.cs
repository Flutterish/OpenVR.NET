using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Buffers;
using System.Runtime.InteropServices;

namespace VisualTests.Graphics;

public class Texture {
	public readonly GlHandle Handle;
	public int Width { get; private set; }
	public int Height { get; private set; }

	public Texture () {
		Handle = GL.GenTexture();
	}

	public static Texture CreateWhitePixel () {
		var tx = new Texture();
		GL.BindTexture( TextureTarget.Texture2D, tx.Handle );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest );

		tx.Width = 1;
		tx.Height = 1;
		GL.TexImage2D( TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new byte[] { 255, 255, 255, 255 } );
		return tx;
	}

	public void Resize ( int width, int height ) {
		GL.BindTexture( TextureTarget.Texture2D, Handle );
		Width = width;
		Height = height;
		GL.TexImage2D( TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero );
	}

	public void Upload ( Image<Rgba32> image ) {
		GL.BindTexture( TextureTarget.Texture2D, Handle );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest );

		Width = image.Width;
		Height = image.Height;
		var data = ArrayPool<byte>.Shared.Rent( image.Width * image.Height * 4 );

		image.ProcessPixelRows( rows => {
			var stride = Width * 4;
			for ( int y = 0; y < Height; y++ ) {
				var span = rows.GetRowSpan( y );
				var byteSpan = MemoryMarshal.CreateReadOnlySpan( ref span[0].R, stride );
				byteSpan.CopyTo( data.AsSpan( y * stride ) );
			}
		} );
		GL.TexImage2D( TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data );

		ArrayPool<byte>.Shared.Return( data );
		image.Dispose();
	}

	public void Upload ( IntPtr ptr, int width, int height ) {
		GL.BindTexture( TextureTarget.Texture2D, Handle );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest );
		GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest );

		Width = width;
		Height = height;
		GL.TexImage2D( TextureTarget2d.Texture2D, 0, TextureComponentCount.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
	}

	public void Upload ( string path ) {
		var image = Image.Load<Rgba32>( path );
		image.Mutate( x => x.Flip( FlipMode.Vertical ) );

		Upload( image );
	}

	public void Bind ( TextureUnit unit = TextureUnit.Texture0 ) {
		GL.ActiveTexture( unit );
		GL.BindTexture( TextureTarget.Texture2D, Handle );
	}
}
