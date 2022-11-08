namespace OpenVR.NET.Devices;

public class Headset : VrDevice {
	protected Headset ( VR vr, int index ) : base( vr, index ) { }
	public Headset ( VR vr, int index, out Owner owner ) : base( vr, index, out owner ) { }
}
