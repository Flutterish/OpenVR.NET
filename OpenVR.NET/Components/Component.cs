using System.Text;
using Valve.VR;

namespace OpenVR.NET.Components {
	public class Component {
		public uint DeviceIndex { get; init; }

		private ComponentModel? model;
		public ComponentModel? Model {
			get {
				if ( model is not null ) return model;

				var sb = new StringBuilder( 256 );
				var error = ETrackedPropertyError.TrackedProp_Success;
				VR.CVRSystem.GetStringTrackedDeviceProperty( DeviceIndex, ETrackedDeviceProperty.Prop_RenderModelName_String, sb, 256, ref error );

				if ( error == ETrackedPropertyError.TrackedProp_Success ) {
					return model = new ComponentModel { Name = sb.ToString() };
				}
				else {
					Events.Error( $"Couldn't find a model for {nameof(Component)} (Device Index: {DeviceIndex}). Using fallback..." );
					try {
						return model = GetFallbackModel();
					}
					finally {
						if ( model is null ) Events.Error( $"Fallback model for {nameof( Component )} could not be found. (Device Index: {DeviceIndex})." );
					}
				}
			}
		}

		protected virtual ComponentModel? GetFallbackModel () => null;
	}
}
