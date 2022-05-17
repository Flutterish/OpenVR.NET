namespace OpenVR.NET.Devices;

public class Headset : VrDevice {
	public Headset ( VR vr, int index, out Owner owner ) : base( vr, index, out owner ) { }
}
