namespace OpenVR.NET.Devices;

/// <summary>
/// Tracker such as a full body tracking device
/// </summary>
public class Tracker : VrDevice {
	protected Tracker ( VR vr, int index ) : base( vr, index ) { }
	public Tracker ( VR vr, int index, out Owner owner ) : base( vr, index, out owner ) { }
}
