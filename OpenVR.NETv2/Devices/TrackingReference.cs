namespace OpenVR.NET.Devices;

/// <summary>
/// Camera and base stations that serve as tracking reference points
/// </summary>
public class TrackingReference : VrDevice {
	public TrackingReference ( VR vr, int index, out Owner owner ) : base( vr, index, out owner ) { }
}
