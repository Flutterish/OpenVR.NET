using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Valve.VR;

namespace OpenVR.NET {
	public class ComponentModel {
		[MaybeNull, NotNull]
		public string Name { get; init; }

		private static bool loadLock = false;
		public async Task LoadAsync ( System.Action? begin = null, System.Action? finish = null, System.Action<Vector3>? addVertice = null, System.Action<Vector2>? addTextureCoordinate = null, System.Action<short, short, short>? addTriangle = null ) {
			while ( loadLock ) {
				await Task.Delay( 100 );
			}
			loadLock = true;
			begin?.Invoke();
			IntPtr ptr = IntPtr.Zero;
			while ( true ) {
				var error = Valve.VR.OpenVR.RenderModels.LoadRenderModel_Async( Name, ref ptr );
				if ( error == EVRRenderModelError.Loading ) {
					await Task.Delay( 100 );
				}
				else if ( error == EVRRenderModelError.None ) {
					RenderModel_t model = new RenderModel_t();

					if ( ( System.Environment.OSVersion.Platform == System.PlatformID.MacOSX ) || ( System.Environment.OSVersion.Platform == System.PlatformID.Unix ) ) {
						var packedModel = (RenderModel_t_Packed)Marshal.PtrToStructure( ptr, typeof( RenderModel_t_Packed ) )!;
						packedModel.Unpack( ref model );
					}
					else {
						model = (RenderModel_t)Marshal.PtrToStructure( ptr, typeof( RenderModel_t ) )!;
					}

					var type = typeof( RenderModel_Vertex_t );
					for ( int iVert = 0; iVert < model.unVertexCount; iVert++ ) {
						var ptr2 = new System.IntPtr( model.rVertexData.ToInt64() + iVert * Marshal.SizeOf( type ) );
						var vert = (RenderModel_Vertex_t)Marshal.PtrToStructure( ptr2, type )!;

						addVertice?.Invoke( new Vector3( vert.vPosition.v0, vert.vPosition.v1, -vert.vPosition.v2 ) );
						addTextureCoordinate?.Invoke( new Vector2( 0, 0 ) );
					}

					int indexCount = (int)model.unTriangleCount * 3;
					var indices = new short[ indexCount ];
					Marshal.Copy( model.rIndexData, indices, 0, indices.Length );

					for ( int iTri = 0; iTri < model.unTriangleCount; iTri++ ) {
						addTriangle?.Invoke( indices[ iTri * 3 + 2 ], indices[ iTri * 3 + 1 ], indices[ iTri * 3 + 0 ] );
					}
					// TODO load textures
					// https://github.com/ValveSoftware/steamvr_unity_plugin/blob/9cc1a76226648d8deb7f3900ab277dfaaa80d60c/Assets/SteamVR/Scripts/SteamVR_RenderModel.cs#L377
					finish?.Invoke();
					loadLock = false;
					return;
				}
				else {
					Events.Error( $"Model `{Name}` could not be loaded." );
					finish?.Invoke();
					loadLock = false;
					return;
				}
			}
		}
	}
}
