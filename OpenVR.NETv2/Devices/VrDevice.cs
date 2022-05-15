using System.Numerics;
using System.Text;
using Valve.VR;

namespace OpenVR.NET.Devices;

public class VrDevice {
	protected VrDevice ( VR vr, int index ) { VR = vr; DeviceIndex = (uint)index; }
	public VrDevice ( VR vr, int index, out Owner owner ) {
		VR = vr;
		DeviceIndex = (uint)index;
		owner = new() { Device = this };
	}

	public readonly VR VR;
	public readonly uint DeviceIndex;
	public Vector3 Position { get; protected set; }
	public Quaternion Rotation { get; protected set; }
	public Vector3 RenderPosition { get; protected set; }
	public Quaternion RenderRotation { get; protected set; }

	private bool isEnabled = false;
	public bool IsEnabled {
		get => isEnabled;
		protected set {
			if ( value == isEnabled )
				return;

			isEnabled = value;
			if ( value )
				Enabled?.Invoke();
			else
				Disabled?.Invoke();
		}
	}

	/// <summary>
	/// Called when device is turned on, on the update thread
	/// </summary>
	public event Action? Enabled;
	/// <summary>
	/// Called when device is turned off, on the update thread
	/// </summary>
	public event Action? Disabled;

	ETrackingResult trackingState;
	public ETrackingResult TrackingState {
		get => trackingState;
		protected set {
			if ( value == trackingState )
				return;

			trackingState = value;
			TrackingStateChanged?.Invoke( value );
		}
	}

	/// <summary>
	/// Called when device changes tracking state, on the update thread
	/// </summary>
	public event Action<ETrackingResult>? TrackingStateChanged;

	DeviceModel? model;
	public DeviceModel? Model {
		get {
			if ( model != null )
				return model;

			var sb = new StringBuilder( 256 );
			var error = ETrackedPropertyError.TrackedProp_Success;
			VR.CVR.GetStringTrackedDeviceProperty( DeviceIndex, ETrackedDeviceProperty.Prop_RenderModelName_String, sb, 256, ref error );

			if ( error == ETrackedPropertyError.TrackedProp_Success ) {
				return model = new DeviceModel { Name = sb.ToString() };
			}
			else {
				try {
					model = GetFallbackModel();
				}
				catch ( Exception e ) {
					VR.Events.Exception( e, $"Couldn't find a model or a fallback for {GetType().Name} (device {DeviceIndex})", EventType.ModelNotFound, (error, this) );
					return null;
				}

				if ( model is null ) {
					VR.Events.Log( $"Couldn't find a model or a fallback for {GetType().Name} (device {DeviceIndex})", EventType.ModelNotFound, (error, this) );
				}
				else {
					VR.Events.Log( $"Couldn't find a model for {GetType().Name} (device {DeviceIndex}). Using fallback", EventType.ModelFallbackUsed, (error, this) );
				}

				return model;
			}
		}
	}

	protected virtual DeviceModel? GetFallbackModel () => null;

	public bool GetBool ( ETrackedDeviceProperty property ) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		var value = VR.CVR.GetBoolTrackedDeviceProperty( DeviceIndex, property, ref error );
		if ( error is ETrackedPropertyError.TrackedProp_Success )
			return value;
		else
			throw new ArgumentException( $"Could not get {property} - {error}" );
	}
	public float GetFloat ( ETrackedDeviceProperty property ) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		var value = VR.CVR.GetFloatTrackedDeviceProperty( DeviceIndex, property, ref error );
		if ( error is ETrackedPropertyError.TrackedProp_Success )
			return value;
		else
			throw new ArgumentException( $"Could not get {property} - {error}" );
	}
	public int GetInt ( ETrackedDeviceProperty property ) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		var value = VR.CVR.GetInt32TrackedDeviceProperty( DeviceIndex, property, ref error );
		if ( error is ETrackedPropertyError.TrackedProp_Success )
			return value;
		else
			throw new ArgumentException( $"Could not get {property} - {error}" );
	}
	public ulong GetUlong ( ETrackedDeviceProperty property ) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		var value = VR.CVR.GetUint64TrackedDeviceProperty( DeviceIndex, property, ref error );
		if ( error is ETrackedPropertyError.TrackedProp_Success )
			return value;
		else
			throw new ArgumentException( $"Could not get {property} - {error}" );
	}
	public string GetString ( ETrackedDeviceProperty property ) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		var sb = new StringBuilder( 256 );
		VR.CVR.GetStringTrackedDeviceProperty( DeviceIndex, property, sb, 256, ref error );
		if ( error is ETrackedPropertyError.TrackedProp_Success )
			return sb.ToString();
		else
			throw new ArgumentException( $"Could not get {property} - {error}" );
	}

	/// <summary>
	/// Updates performed on the update thread
	/// </summary>
	public virtual void Update () {

	}

	public bool IsWireless => GetBool( ETrackedDeviceProperty.Prop_DeviceIsWireless_Bool );
	public bool HasBattery => GetBool( ETrackedDeviceProperty.Prop_DeviceProvidesBatteryStatus_Bool );
	/// <summary>
	/// Whether the device is currently on, and can turn off at any time.
	/// This property will be <see langword="false"/> when the device is turned off.
	/// </summary>
	public bool CanPowerOff => GetBool( ETrackedDeviceProperty.Prop_DeviceCanPowerOff_Bool );
	public bool IsCharging => GetBool( ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool );
	public float BatteryLevel => GetFloat( ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float );

	public class Owner {
		public VrDevice Device { get; internal init; } = null!;
		public bool IsEnabled {
			get => Device.IsEnabled;
			set => Device.IsEnabled = value;
		}

		public Vector3 Position {
			get => Device.Position;
			set => Device.Position = value;
		}
		public Quaternion Rotation {
			get => Device.Rotation;
			set => Device.Rotation = value;
		}
		public Vector3 RenderPosition {
			get => Device.RenderPosition;
			set => Device.RenderPosition = value;
		}
		public Quaternion RenderRotation {
			get => Device.RenderRotation;
			set => Device.RenderRotation = value;
		}

		public ETrackingResult offThreadTrackingState;
		public ETrackingResult TrackingState {
			get => Device.TrackingState;
			set => Device.TrackingState = value;
		}
	}
}
