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
	/// <summary>
	/// Position in meters to be used on the input thread
	/// </summary>
	public Vector3 Position { get; protected set; }
	/// <summary>
	/// Velocity in meters per second to be used on the input thread
	/// </summary>
	public Vector3 Velocity { get; protected set; }
	/// <summary>
	/// Rotation quaternion to be used on the input thread
	/// </summary>
	public Quaternion Rotation { get; protected set; }
	/// <summary>
	/// Angular velocity in radians per second (?) to be used on the input thread <see href="https://github.com/ValveSoftware/openvr/blob/4c85abcb7f7f1f02adaf3812018c99fc593bc341/headers/openvr.h#L260"/>
	/// </summary>
	public Vector3 AngularVelocity { get; protected set; }
	/// <summary>
	/// Position in meters to be used on the draw thread
	/// </summary>
	public Vector3 RenderPosition { get; protected set; }
	/// <summary>
	/// Rotation quaternion to be used on the draw thread
	/// </summary>
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

	protected DeviceModel? model;
	public DeviceModel? Model {
		get {
			if ( model != null )
				return model;

			var sb = new StringBuilder( 256 );
			var error = ETrackedPropertyError.TrackedProp_Success;
			VR.CVR.GetStringTrackedDeviceProperty( DeviceIndex, ETrackedDeviceProperty.Prop_RenderModelName_String, sb, 256, ref error );

			if ( error == ETrackedPropertyError.TrackedProp_Success ) {
				return model = new DeviceModel( sb.ToString() );
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
	public T GetEnum<T> ( ETrackedDeviceProperty property ) where T : struct, Enum {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		var value = VR.CVR.GetInt32TrackedDeviceProperty( DeviceIndex, property, ref error );
		if ( error is ETrackedPropertyError.TrackedProp_Success )
			return (T)(object)value;
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
		var length = VR.CVR.GetStringTrackedDeviceProperty( DeviceIndex, property, sb, 256, ref error );
		if ( error is ETrackedPropertyError.TrackedProp_Success )
			return sb.ToString();
		else if ( error is ETrackedPropertyError.TrackedProp_BufferTooSmall ) {
			sb.EnsureCapacity( (int)length );
			VR.CVR.GetStringTrackedDeviceProperty( DeviceIndex, property, sb, length, ref error );
			return sb.ToString();
		}
		else
			throw new ArgumentException( $"Could not get {property} - {error}" );
	}
	public object? GetProperty ( ETrackedDeviceProperty property ) {
		if ( property.ToString().EndsWith( "_Bool" ) )
			return GetBool( property );
		else if ( property.ToString().EndsWith( "_Float" ) )
			return GetFloat( property );
		else if ( property.ToString().EndsWith( "_Int32" ) )
			return GetInt( property );
		else if ( property.ToString().EndsWith( "_Uint64" ) )
			return GetUlong( property );
		else if ( property.ToString().EndsWith( "_String" ) )
			return GetString( property );

		return null;
	}

	public bool HasProperty ( ETrackedDeviceProperty property ) {
		ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
		VR.CVR.GetBoolTrackedDeviceProperty( DeviceIndex, property, ref error );

		return error is not ETrackedPropertyError.TrackedProp_UnknownProperty;
	}

	public bool IsWireless => GetBool( ETrackedDeviceProperty.Prop_DeviceIsWireless_Bool );
	public bool HasBattery => GetBool( ETrackedDeviceProperty.Prop_DeviceProvidesBatteryStatus_Bool );
	/// <summary>
	/// Whether the device is currently on, and can turn off at any time.
	/// This property will be <see langword="false"/> when the device is turned off.
	/// </summary>
	public bool CanPowerOff => GetBool( ETrackedDeviceProperty.Prop_DeviceCanPowerOff_Bool );
	public bool IsCharging => GetBool( ETrackedDeviceProperty.Prop_DeviceIsCharging_Bool );
	/// <summary>
	/// Battery level as a percentage in the range [0; 1]
	/// </summary>
	public float BatteryLevel => GetFloat( ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float );

	/// <summary>
	/// Updates performed on the update thread
	/// </summary>
	public virtual void Update () {
		Activity = VR.CVR.GetTrackedDeviceActivityLevel( DeviceIndex );
	}

	EDeviceActivityLevel activity = EDeviceActivityLevel.k_EDeviceActivityLevel_Unknown;
	public EDeviceActivityLevel Activity {
		get => activity;
		private set {
			if ( activity == value )
				return;

			activity = value;
			ActivityChanged?.Invoke( value );
		}
	}

	/// <summary>
	/// Called when device changes activity level, on the update thread
	/// </summary>
	public event Action<EDeviceActivityLevel>? ActivityChanged;

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
		public Vector3 Velocity {
			get => Device.Velocity;
			set => Device.Velocity = value;
		}
		public Quaternion Rotation {
			get => Device.Rotation;
			set => Device.Rotation = value;
		}
		public Vector3 AngularVelocity {
			get => Device.AngularVelocity;
			set => Device.AngularVelocity = value;
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
