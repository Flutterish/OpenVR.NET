namespace OpenVR.NET.Devices;

/// <summary>
/// Tracker such as a full body tracking device
/// </summary>
public class Tracker : VrDevice {
	public Tracker ( VR vr, int index, out Owner owner ) : base( vr, index, out owner ) { }
}
