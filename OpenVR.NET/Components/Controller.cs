using OpenVR.NET.Components;
using OpenVR.NET.Manifests;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Valve.VR;

namespace OpenVR.NET {
	public class Controller : Component {
		/// <summary>
		/// Whether this controller is the main one, for example if someone is right handed and this is the right controller.
		/// </summary>
		public bool IsMainController { get; init; }
		public ETrackedControllerRole Role { get; init; }

		public Vector3 Position;
		public Quaternion Rotation;

		private bool isEnabled;
		public bool IsEnabled {
			get => isEnabled;
			set {
				if ( isEnabled == value ) return;
				isEnabled = value;
				if ( isEnabled )
					Enabled?.Invoke();
				else
					Disabled?.Invoke();
			}
		}
		public event System.Action Enabled;
		public event System.Action Disabled;

		public void BindEnabled ( System.Action action, bool runNowIfEnabled = false ) {
			Enabled += action;
			if ( runNowIfEnabled && IsEnabled ) action();
		}
		public void BindDisabled ( System.Action action, bool runNowIfDisabled = false ) {
			Disabled += action;
			if ( runNowIfDisabled && !IsEnabled ) action();
		}

		public ulong Handle { get; init; }

		/// <summary>
		/// Retreives a controller component for a given name declared in the manifest via <see cref="VR.SetManifest(Manifest)"/>.
		/// The generic type should be one of <see cref="ControllerButton"/>, <see cref="ControllerVector"/>, <see cref="Controller2DVector"/>, <see cref="Controller3DVector"/> or <see cref="ControllerHaptic"/>.
		/// </summary>
		public T GetComponent<T> ( object name ) where T : ControllerComponent
			=> VR.GetControllerComponent<T>( name, this );

		const string DEFAULT_CONTROLLER_MODEL = "{indexcontroller}valve_controller_knu_1_0_left";
		protected override ComponentModel GetFallbackModel () {
			Events.Error( $"Fallback Model: Valve Index Controller (Left)" );
			return new ComponentModel() { Name = DEFAULT_CONTROLLER_MODEL };
		}
	}
}
