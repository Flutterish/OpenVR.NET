using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Valve.VR;

namespace OpenVR.NET.Manifests {
	/// <summary>
	/// Represents an input or output of a controller
	/// </summary>
	public abstract class ControllerComponent {
		/// <summary>
		/// A unique name for this component, usually an enum or a string
		/// </summary>
		public object Name { get; init; }
		public ulong Handle { get; init; }
		protected ulong Restriction { get; init; } = Valve.VR.OpenVR.k_ulInvalidActionHandle;

		/// <summary>
		/// A once per input frame action that is called from <see cref="VR"/>
		/// </summary>
		public abstract void Update ();
		internal abstract ControllerComponent CopyWithRestriction ( ulong mask );
	}

	public readonly struct ValueUpdatedEvent<T> {
		public readonly T NewValue;
		public readonly T OldValue;

		public readonly Controller Source;

		public ValueUpdatedEvent ( T newValue, T oldValue, Controller source ) {
			NewValue = newValue;
			OldValue = oldValue;
			Source = source;
		}
	}

	public abstract class ControllerInputComponent<T> : ControllerComponent where T : IEquatable<T> {
		private T value;
		protected ulong SourceHandle;
		public T Value {
			get => value;
			protected set {
				var old = this.value;
				var @event = new ValueUpdatedEvent<T>( value, old, VR.ControllerForOrigin( SourceHandle ) );
				ValueUpdated?.Invoke( @event );
				if ( value.Equals( this.value ) ) return;
				this.value = value;
				ValueChanged?.Invoke( @event );
			}
		}

		/// <summary>
		/// Value was updated.
		/// </summary>
		public event System.Action<ValueUpdatedEvent<T>> ValueUpdated;
		/// <summary>
		/// Binds an event to run when the value was updated.
		/// </summary>
		public void BindValueUpdatedDetailed ( System.Action<ValueUpdatedEvent<T>> action, bool runOnceImmediately = false ) {
			ValueUpdated += action;
			if ( runOnceImmediately ) action( new( value, value, VR.ControllerForOrigin( SourceHandle ) ) );
		}
		/// <summary>
		/// Binds an event to run when the value was updated.
		/// </summary>
		public void BindValueUpdated ( System.Action<T> action, bool runOnceImmediately = false ) {
			ValueUpdated += v => action(v.NewValue);
			if ( runOnceImmediately ) action( value );
		}

		/// <summary>
		/// Value was updated and is different from the previous one.
		/// </summary>
		public event System.Action<ValueUpdatedEvent<T>> ValueChanged;
		/// <summary>
		/// Binds an event to run when the value was updated and was different from the previous one.
		/// </summary>
		public void BindValueChangedDetailed ( System.Action<ValueUpdatedEvent<T>> action, bool runOnceImmediately = false ) {
			ValueChanged += action;
			if ( runOnceImmediately ) action( new( value, value, VR.ControllerForOrigin( SourceHandle ) ) );
		}
		/// <summary>
		/// Binds an event to run when the value was updated and was different from the previous one.
		/// </summary>
		public void BindValueChanged ( System.Action<T> action, bool runOnceImmediately = false ) {
			ValueChanged += v => action(v.NewValue);
			if ( runOnceImmediately ) action( value );
		}
	}

	public class ControllerButton : ControllerInputComponent<bool> {
		public override void Update () {
			InputDigitalActionData_t data = default;
			var error = Valve.VR.OpenVR.Input.GetDigitalActionData( Handle, ref data, (uint)Marshal.SizeOf<InputDigitalActionData_t>(), Restriction );
			if ( error != EVRInputError.None ) {
				Events.Error( $"Cannot read input: {error}" );
				return;
			}

			SourceHandle = data.activeOrigin;
			Value = data.bState;
		}

		internal override ControllerComponent CopyWithRestriction ( ulong mask ) {
			return new ControllerButton {
				Handle = Handle,
				Name = Name,
				Restriction = mask,
				Value = Value
			};
		}
	}

	public class ControllerVector : ControllerInputComponent<float> {
		public override void Update () {
			InputAnalogActionData_t data = default;
			var error = Valve.VR.OpenVR.Input.GetAnalogActionData( Handle, ref data, (uint)Marshal.SizeOf<InputAnalogActionData_t>(), Restriction );
			if ( error != EVRInputError.None ) {
				Events.Error( $"Cannot read input: {error}" );
				return;
			}

			SourceHandle = data.activeOrigin;
			Value = data.x;
		}

		internal override ControllerComponent CopyWithRestriction ( ulong mask ) {
			return new ControllerVector {
				Handle = Handle,
				Name = Name,
				Restriction = mask,
				Value = Value
			};
		}
	}

	public class Controller2DVector : ControllerInputComponent<Vector2> {
		public override void Update () {
			InputAnalogActionData_t data = default;
			var error = Valve.VR.OpenVR.Input.GetAnalogActionData( Handle, ref data, (uint)Marshal.SizeOf<InputAnalogActionData_t>(), Restriction );
			if ( error != EVRInputError.None ) {
				Events.Error( $"Cannot read input: {error}" );
				return;
			}

			SourceHandle = data.activeOrigin;
			Value = new Vector2( data.x, data.y );
		}

		internal override ControllerComponent CopyWithRestriction ( ulong mask ) {
			return new Controller2DVector {
				Handle = Handle,
				Name = Name,
				Restriction = mask,
				Value = Value
			};
		}
	}

	public class Controller3DVector : ControllerInputComponent<Vector3> {
		public override void Update () {
			InputAnalogActionData_t data = default;
			var error = Valve.VR.OpenVR.Input.GetAnalogActionData( Handle, ref data, (uint)Marshal.SizeOf<InputAnalogActionData_t>(), Restriction);
			if ( error != EVRInputError.None ) {
				Events.Error( $"Cannot read input: {error}" );
				return;
			}

			SourceHandle = data.activeOrigin;
			Value = new Vector3( data.x, data.y, data.z );
		}

		internal override ControllerComponent CopyWithRestriction ( ulong mask ) {
			return new Controller3DVector {
				Handle = Handle,
				Name = Name,
				Restriction = mask,
				Value = Value
			};
		}
	}

	public abstract class ControllerOutputComponent : ControllerComponent {
		public override void Update () { }
	}

	public class ControllerHaptic : ControllerOutputComponent {
		/// <summary>
		/// Triggers a haptic vibration.
		/// Paramerters are in seconds and hertz.
		/// </summary>
		public void TriggerVibration ( double duration, double frequency = 5, double amplitude = 0.5, double delay = 0 ) { // BUG doesnt work?
			var error = Valve.VR.OpenVR.Input.TriggerHapticVibrationAction( Handle, (float)delay, (float)duration, (float)frequency, (float)amplitude, Restriction );
			if ( error != EVRInputError.None ) {
				Events.Error( $"Cannot send haptic vibration: {error}." );
			}
		}

		internal override ControllerComponent CopyWithRestriction ( ulong mask ) {
			return new ControllerHaptic {
				Handle = Handle,
				Name = Name,
				Restriction = mask
			};
		}
	}
}
