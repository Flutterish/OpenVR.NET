using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using Valve.VR;

namespace OpenVR.NET.Devices;

public class DeviceModel {
	public string Name { get; init; } = string.Empty;
	static readonly SemaphoreSlim loadSemaphore = new( 1, 1 );

	public delegate void AddVertice ( Vector3 position, Vector3 normal, Vector2 uv );

	/// <summary>
	/// Loads the model asynchronously (on a thread pool).
	/// If a callback is not registered, the associated resource will not be loaded,
	/// otherwise it is your responsibility to dispose of any received disposable resources.
	/// </summary>
	public async Task LoadAsync (
		Action? begin = null,
		Action? finish = null,
		AddVertice? addVertice = null,
		Action<short, short, short>? addTriangle = null,
		Action<Image<Rgba32>>? addTexture = null,
		Action<EVRRenderModelError>? onError = null ) {

		// TODO figure out if we can load different models at the same time
		// (last time there were problems, but it might just be an issue on my side)
		//	~Peri
		await loadSemaphore.WaitAsync();

		begin?.Invoke();
		IntPtr modelPtr = IntPtr.Zero;
		EVRRenderModelError error;
		while ( ( error = Valve.VR.OpenVR.RenderModels.LoadRenderModel_Async( Name, ref modelPtr ) ) is EVRRenderModelError.Loading ) {
			await Task.Delay( 10 );
		}

		if ( error is EVRRenderModelError.None ) {
			RenderModel_t model = new();

			if ( Environment.OSVersion.Platform is PlatformID.MacOSX or PlatformID.Unix ) {
				var packedModel = Marshal.PtrToStructure<RenderModel_t_Packed>( modelPtr );
				packedModel.Unpack( ref model );
			}
			else {
				model = Marshal.PtrToStructure<RenderModel_t>( modelPtr );
			}

			if ( addVertice != null ) {
				var size = Marshal.SizeOf<RenderModel_Vertex_t>();
				for ( int i = 0; i < model.unVertexCount; i++ ) {
					var vertPtr = model.rVertexData + i * size;
					var vert = Marshal.PtrToStructure<RenderModel_Vertex_t>( vertPtr );

					addVertice.Invoke(
						new Vector3( vert.vPosition.v0, vert.vPosition.v1, -vert.vPosition.v2 ),
						new Vector3( vert.vNormal.v0, vert.vNormal.v1, -vert.vNormal.v2 ),
						new Vector2( vert.rfTextureCoord0, 1 - vert.rfTextureCoord1 )
					);
				}
			}

			if ( addTriangle != null ) {
				int indexCount = (int)model.unTriangleCount * 3;
				var indices = ArrayPool<short>.Shared.Rent( indexCount );
				Marshal.Copy( model.rIndexData, indices, 0, indexCount );
				for ( int i = 0; i < model.unTriangleCount; i++ ) {
					addTriangle.Invoke( indices[i * 3 + 2], indices[i * 3 + 1], indices[i * 3 + 0] );
				}
				ArrayPool<short>.Shared.Return( indices );
			}

			if ( addTexture != null ) {
				IntPtr texturePtr = IntPtr.Zero;
				while ( ( error = Valve.VR.OpenVR.RenderModels.LoadTexture_Async( model.diffuseTextureId, ref texturePtr ) ) is EVRRenderModelError.Loading ) {
					await Task.Delay( 10 );
				}

				if ( error is EVRRenderModelError.None ) {
					RenderModel_TextureMap_t texture = new();
					if ( Environment.OSVersion.Platform is PlatformID.MacOSX or PlatformID.Unix ) {
						var packedModel = Marshal.PtrToStructure<RenderModel_TextureMap_t_Packed>( texturePtr );
						packedModel.Unpack( ref texture );
					}
					else {
						texture = Marshal.PtrToStructure<RenderModel_TextureMap_t>( texturePtr );
					}

					var data = new byte[texture.unWidth * texture.unHeight * 4];
					Marshal.Copy( texture.rubTextureMapData, data, 0, data.Length );
					Image<Rgba32> image = new( texture.unWidth, texture.unHeight );

					image.ProcessPixelRows( rows => {
						int i = 0;
						for ( int y = 0; y < texture.unHeight; y++ ) {
							var span = rows.GetRowSpan( y );
							for ( int x = 0; x < texture.unWidth; x++ ) {
								span[x] = new( data[i++], data[i++], data[i++], data[i++] );
							}
						}
					} );

					addTexture( image );
					Valve.VR.OpenVR.RenderModels.FreeTexture( texturePtr );
				}
				else {
					onError?.Invoke( error );
				}
			}

			Valve.VR.OpenVR.RenderModels.FreeRenderModel( modelPtr );
			finish?.Invoke();
		}
		else {
			onError?.Invoke( error );
			finish?.Invoke();
		}

		loadSemaphore.Release();
	}
}
