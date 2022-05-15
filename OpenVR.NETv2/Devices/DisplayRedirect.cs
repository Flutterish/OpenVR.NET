namespace OpenVR.NET.Devices;

/// <summary>
/// Accessories that aren't necessarily tracked themselves, but may redirect video output from other tracked devices
/// </summary>
public class DisplayRedirect : VrDevice {
	public DisplayRedirect ( VR vr, int index, out Owner owner ) : base( vr, index, out owner ) { }
}
