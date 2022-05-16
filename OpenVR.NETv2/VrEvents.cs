using OpenVR.NET.Devices;
using Valve.VR;

namespace OpenVR.NET;

/// <summary>
/// VR event logger.
/// </summary>
public class VrEvents {
	public void Log ( string message, EventType type, object? context = null ) {
		OnLog?.Invoke( message, type, context );
	}
	public delegate void OpenVrEventHandler ( EVREventType type, VrDevice? device, float age, in VREvent_Data_t data );
	public void Event ( EVREventType type, VrDevice? device, float age, in VREvent_Data_t data ) {
		OnOpenVrEvent?.Invoke( type, device, age, data );
	}
	public void Exception ( Exception e, string messgae, EventType type, object? context = null ) {
		OnException?.Invoke( messgae, type, context, e );
	}

	public bool AnyOnOpenVrEventHandlers => OnOpenVrEvent is not null;

	/// <summary>
	/// Log from within the OpenVR.NET library. Called on related threads.
	/// </summary>
	public event Action<string, EventType, object?>? OnLog;
	/// <summary>
	/// OpenVR events. Called on the update thread.
	/// Events will not be polled unless this has subscribers
	/// </summary>
	public event OpenVrEventHandler? OnOpenVrEvent;
	/// <summary>
	/// Exceptions from within the OpenVR.NET library. Called on related threads.
	/// </summary>
	public event Action<string, EventType, object?, Exception>? OnException;
}

public enum EventType {
	InitializationSuccess,
	InitializationError,

	CouldntFetchDominantHand,
	CoundntFetchPlayerPose,
	CoundntFetchControllerRole,
	CoundntFetchControllerHandle,
	CoundntFetchControllerButtons,
	CoundntFetchControllerAxis,

	ModelNotFound,
	ModelFallbackUsed,

	FrameSubmitError,

	NoFous,

	VrEvent
}