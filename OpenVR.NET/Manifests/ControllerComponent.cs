using System;
using System.Runtime.InteropServices;
using Valve.VR;

namespace OpenVR.NET.Manifests {
	/// <summary>
	/// Represents an input or output of a controller
	/// </summary>
	public abstract class ControllerComponent {
		/// <summary>
		/// A unique name for this component, usually an enum
		/// </summary>
		public object Name { get; init; }
		public ulong Handle { get; init; }

		public abstract void Update ();
	}

	public abstract class ControllerComponent<T> : ControllerComponent where T : IEquatable<T> {
		private T value;
		public T Value {
			get => value;
			protected set {
				if ( value.Equals( this.value ) ) return;
				this.value = value;
				ValueChanged?.Invoke( value );
			}
		}

		public event System.Action<T> ValueChanged;
		public void BindValueChanged ( System.Action<T> action, bool runOnceImmediately = false ) {
			ValueChanged += action;
			if ( runOnceImmediately ) action( value );
		}
	}

	// TODO leftright actions can be restricted to a given side
	public class ControllerButton : ControllerComponent<bool> {
		public override void Update () {
			InputDigitalActionData_t data = default;
			var error = Valve.VR.OpenVR.Input.GetDigitalActionData( Handle, ref data, (uint)Marshal.SizeOf<InputDigitalActionData_t>(), Valve.VR.OpenVR.k_ulInvalidActionHandle );
			if ( error != EVRInputError.None ) {
				Events.Error( $"Cannot read input: {error}" );
				return;
			}

			Value = data.bState;
		}
	}
}
