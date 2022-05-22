namespace OpenVR.NET.Input;

/// <summary>
/// An action bound to a device component such as a button, a joystick or a haptic.
/// An action can be bound to both controllers at once, or just one.
/// Action events are invoked on the input thread
/// </summary>
public abstract class Action {
	/// <summary>
	/// Updates the state of this action, on the input thread
	/// </summary>
	public abstract void Update ();

	public ulong DeviceHandle { get; init; } = Valve.VR.OpenVR.k_ulInvalidActionHandle;
	public ulong SourceHandle { get; init; } = Valve.VR.OpenVR.k_ulInvalidInputValueHandle;
}
