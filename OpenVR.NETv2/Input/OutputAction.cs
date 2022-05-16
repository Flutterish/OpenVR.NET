using Valve.VR;

namespace OpenVR.NET.Input;

/// <inheritdoc/>
public abstract class OutputAction : Action {
	public override void Update () { }
}

public class HapticAction : OutputAction {
	/// <summary>
	/// Triggers a haptic vibration.
	/// Paramerters are in seconds and hertz.
	/// </summary>
	public bool TriggerVibration ( double duration, double frequency = 40, double amplitude = 1, double delay = 0 ) {
		if ( DeviceHandle == Valve.VR.OpenVR.k_ulInvalidActionHandle )
			return false;
		return Valve.VR.OpenVR.Input.TriggerHapticVibrationAction(
			SourceHandle, (float)delay, (float)duration,
			(float)frequency, (float)amplitude, DeviceHandle
		) is EVRInputError.None;
	}
}