using OpenVR.NET.Manifests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Valve.VR;

namespace OpenVR.NET {
	public static class VR {
		// NOTE platform specific binaries for openVR are there: https://github.com/ValveSoftware/openvr/tree/master/bin
		private static VrState vrState = VrState.NotInitialized;
		public static VrState VrState { 
			get => vrState; 
			private set {
				if ( vrState == value ) return;

				vrState = value;
				VrStateChanged?.Invoke( value );
			}
		}
		public static event System.Action<VrState> VrStateChanged;
		public static void BindVrStateChanged ( System.Action<VrState> action, bool runOnceImmediately = false ) {
			VrStateChanged += action;
			if ( runOnceImmediately ) action( vrState );
		}

		public static CVRSystem CVRSystem { get; private set; }
		public static Vector2 RenderSize { get; private set; }

		private static double lastInitializationAttempt;
		private static double initializationAttemptInterval = 5000;
		public static readonly VrInput Current = new();
		/// <summary>
		/// Initializes and draws to the headset. Should be called on the draw thread. Blocks to synchronize with headset refresh rate.
		/// </summary>
		/// <param name="timeMS">Timestamp in milliseconds</param>
		public static void UpdateDraw ( double timeMS ) { // TODO make sure events are called on update thread
			lastTime = now;
			now = timeMS;

			if ( VrState.HasFlag( VrState.NotInitialized ) && timeMS >= lastInitializationAttempt + initializationAttemptInterval ) {
				lastInitializationAttempt = timeMS;
				EVRInitError error = EVRInitError.None;
				CVRSystem = Valve.VR.OpenVR.Init( ref error );
				if ( error == EVRInitError.None ) {
					VrState = VrState.OK;
					Events.Log( "OpenVR initialzed succesfuly" );
					InitializeOpenVR();
				}
				else {
					Events.Error( $"OpenVR could not be initialized: {error.GetReadableDescription()}" );
				}
			}

			if ( VrState.HasFlag( VrState.OK ) ) {
				// TODO check if rig is still alive
				ReadVrPoses();
			}
		}

		private static double lastTime;
		private static double now;
		/// <summary>
		/// How much time has passed since last draw frame.
		/// </summary>
		public static double DeltaTime => (now - lastTime)/1000;
		public static ETrackedControllerRole DominantHand { get; private set; } = ETrackedControllerRole.Invalid;
		static void InitializeOpenVR () {
			// TODO log/explain startup errors https://github.com/ValveSoftware/openvr/wiki/API-Documentation
			uint w = 0, h = 0;
			CVRSystem.GetRecommendedRenderTargetSize( ref w, ref h );
			RenderSize = new Vector2( w, h );

			if ( ActionManifest is not null ) SetManifest();
		}

		const string DEFAULT_CONTROLLER_MODEL = "{indexcontroller}valve_controller_knu_1_0_left";
		private static readonly TrackedDevicePose_t[] trackedRenderDevices = new TrackedDevicePose_t[ Valve.VR.OpenVR.k_unMaxTrackedDeviceCount ];
		private static readonly TrackedDevicePose_t[] trackedGameDevices = new TrackedDevicePose_t[ Valve.VR.OpenVR.k_unMaxTrackedDeviceCount ];
		static void ReadVrPoses () {
			// this blocks on the draw thread but it needs to be there ( it limits fps to headset framerate so its fine )
			var error = Valve.VR.OpenVR.Compositor.WaitGetPoses( trackedRenderDevices, trackedGameDevices );
			if ( error != EVRCompositorError.None ) {
				Events.Error( $"Pose error: {error}" );
				return;
			}

			for ( int i = 0; i < trackedRenderDevices.Length + trackedGameDevices.Length; i++ ) {
				int index = i < trackedRenderDevices.Length ? i : ( i - trackedRenderDevices.Length );
				var device = i < trackedRenderDevices.Length ? trackedRenderDevices[ index ] : trackedGameDevices[ index ];
				if ( device.bPoseIsValid && device.bDeviceIsConnected ) {
					switch ( Valve.VR.OpenVR.System.GetTrackedDeviceClass( (uint)i ) ) {
						case ETrackedDeviceClass.HMD:
							Current.Headset.Position = device.mDeviceToAbsoluteTracking.ExtractPosition();
							Current.Headset.Rotation = device.mDeviceToAbsoluteTracking.ExtractRotation();
							break;

						case ETrackedDeviceClass.Controller:
							if ( !Current.Controllers.ContainsKey( i ) ) {
								var error2 = ETrackedPropertyError.TrackedProp_Success;
								var roleID = Valve.VR.OpenVR.System.GetInt32TrackedDeviceProperty( (uint)index, ETrackedDeviceProperty.Prop_ControllerRoleHint_Int32, ref error2 );

								var role = (ETrackedControllerRole)roleID;
								var sb = new StringBuilder( 256 );
								var propError = ETrackedPropertyError.TrackedProp_Success;
								CVRSystem.GetStringTrackedDeviceProperty( (uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, sb, 256, ref propError );
								// TODO test if this works
								ulong handle = 0;
								Valve.VR.OpenVR.Input.GetInputSourceHandle( role == ETrackedControllerRole.LeftHand ? Valve.VR.OpenVR.k_pchPathUserHandLeft : Valve.VR.OpenVR.k_pchPathUserHandRight, ref handle );
								Controller controller;
								if ( propError != ETrackedPropertyError.TrackedProp_Success ) {
									controller = new() {
										ID = i,
										ModelName = DEFAULT_CONTROLLER_MODEL,
										Role = role,
										IsMainController = role == DominantHand,
										Handle = handle
									};
									Events.Error( $"Couldn't find a model for controller with id {index}. Using Valve Index Controller (Left)" );
								}
								else {
									controller = new() { 
										ID = i, 
										ModelName = sb.ToString(),
										Role = role,
										IsMainController = role == DominantHand,
										Handle = handle
									};
								}
								controller.BindEnabled( () => EnabledControllerCount++, true );
								controller.BindDisabled( () => EnabledControllerCount-- );
								Current.Controllers.Add( i, controller );
								NewControllerAdded?.Invoke( controller );
							}
							Current.Controllers[ i ].Position = device.mDeviceToAbsoluteTracking.ExtractPosition();
							Current.Controllers[ i ].Rotation = device.mDeviceToAbsoluteTracking.ExtractRotation();
							Current.Controllers[ i ].IsEnabled = device.bDeviceIsConnected;
							break;
					}
				}
				else if ( i == index ) {
					if ( Current.Controllers.ContainsKey( i ) ) {
						Current.Controllers[ i ].IsEnabled = false;
					}
				}
			}
		}
		public static Controller ControllerForOrigin ( ulong origin ) {
			InputOriginInfo_t info = default;
			Valve.VR.OpenVR.Input.GetOriginTrackedDeviceInfo( origin, ref info, (uint)Marshal.SizeOf<InputOriginInfo_t>() );
			return Current.Controllers.TryGetValue( (int)info.trackedDeviceIndex, out var c ) ? c : null;
		}
		/// <summary>
		/// The main controller or if not present, a fallback one
		/// </summary>
		public static Controller MainController => Current.Controllers.Values.FirstOrDefault( x => x.IsEnabled && x.IsMainController ) ?? Current.Controllers.Values.FirstOrDefault( x => x.IsEnabled );
		public static Controller SecondaryController {
			get {
				var main = MainController;
				return Current.Controllers.Values.FirstOrDefault( x => x!= main && x.IsEnabled );
			}
		}
		public static Controller LeftController => Current.Controllers.Values.FirstOrDefault( x => x.Role == ETrackedControllerRole.LeftHand );
		public static Controller RightController => Current.Controllers.Values.FirstOrDefault( x => x.Role == ETrackedControllerRole.RightHand );
		public static int EnabledControllerCount { get; private set; }
		public static event System.Action<Controller> NewControllerAdded;
		public static void BindNewControllerAdded ( System.Action<Controller> action, bool runOnExisting = false ) {
			NewControllerAdded += action;
			if ( runOnExisting ) {
				foreach ( var i in Current.Controllers ) {
					action( i.Value );
				}
			}
		}

		/// <summary>
		/// Updates inputs and polls events. Should be called on the input or update thread.
		/// </summary>
		public static void Update () {
			if ( !VrState.HasFlag( VrState.OK ) ) return;
			if ( ActionManifest is null || !AreComponentsLoaded ) return;

			var vrEvents = new List<VREvent_t>();
			var vrEvent = new VREvent_t();
			try {
				while ( Valve.VR.OpenVR.System.PollNextEvent( ref vrEvent, (uint)Marshal.SizeOf<VREvent_t>() ) ) {
					vrEvents.Add( vrEvent );
				}
			}
			catch ( Exception e ) {
				Events.Exception( e, "Could not get events" );
			}

			// Printing events
			foreach ( var e in vrEvents ) {
				var pid = e.data.process.pid;
				if ( (EVREventType)vrEvent.eventType != EVREventType.VREvent_None ) {
					var name = Enum.GetName( typeof( EVREventType ), e.eventType );
					var message = $"[{pid}] {name}";
					if ( pid == 0 ) Events.Log( message );
					else if ( name == null ) Events.Log( message );
					else if ( name.ToLower().Contains( "fail" ) )
						Events.Error( message );
					else if ( name.ToLower().Contains( "error" ) )
						Events.Error( message );
					else if ( name.ToLower().Contains( "success" ) )
						Events.Log( message );
					else Events.Log( message );
				}
			}

			Valve.VR.OpenVR.Input.UpdateActionState( actionSets, (uint)System.Runtime.InteropServices.Marshal.SizeOf<VRActiveActionSet_t>() );
			foreach ( var i in components ) {
				foreach ( var k in i.Value ) {
					k.Value.Update();
				}
			}
		}

		public static void Exit () {
			if ( CVRSystem is not null ) {
				Valve.VR.OpenVR.Shutdown();
				CVRSystem = null;
			}
		}

		public static Manifest ActionManifest { get; private set; }
		public static void SetManifest ( Manifest manifest ) {
			if ( ActionManifest is not null ) {
				throw new InvalidOperationException( $"{nameof(ActionManifest)} is already declared." );
			}
			ActionManifest = manifest;
			if ( VrState.HasFlag( VrState.OK ) ) SetManifest();
		}

		public const string ACTION_MANIFEST_NAME = "openVR_action_manifest.json";
		public const string VR_MANIFEST_NAME = "openVR_vrmanifest.vrmanifest";
		public static bool AreComponentsLoaded { get; private set; }
		static Controller nullController = new();
		static Dictionary<object, Dictionary<Controller, ControllerComponent>> components = new();
		static VRActiveActionSet_t[] actionSets;
		static void SetManifest () {
			var raw = RawManifest.Parse( ActionManifest );
			var actionManifestPath = Path.Combine( Directory.GetCurrentDirectory(), ACTION_MANIFEST_NAME );
			File.WriteAllText( actionManifestPath, System.Text.Json.JsonSerializer.Serialize( raw.actionManifest, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true } ) );
			var vrManifestPath = Path.Combine( Directory.GetCurrentDirectory(), VR_MANIFEST_NAME );
			File.WriteAllText( vrManifestPath, System.Text.Json.JsonSerializer.Serialize( raw.vrManifest, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true } ) );
			var error = Valve.VR.OpenVR.Applications.AddApplicationManifest( vrManifestPath, true );
			if ( error != EVRApplicationError.None ) {
				Events.Error($"Couldn't set application vr manifest: {error}" );
			}
			var error2 = Valve.VR.OpenVR.Input.SetActionManifestPath( actionManifestPath );
			if ( error2 != EVRInputError.None ) {
				Events.Error( $"Couldn't set application action manifest: {error2}" );
			}

			foreach ( var group in ActionManifest.EnumerateGroups() ) {
				foreach ( var action in group.EnumerateActions() ) {
					ulong handle = 0;
					Valve.VR.OpenVR.Input.GetActionHandle( action.FullPath, ref handle );
					var comp = action.CreateComponent( handle );
					components.Add( comp.Name, new() { [ nullController ] = comp } );
				}
			}

			actionSets = ActionManifest.EnumerateGroups().Select( x => {
				ulong handle = 0;
				var error = Valve.VR.OpenVR.Input.GetActionSetHandle( x.FullPath, ref handle );
				if ( error != EVRInputError.None ) {
					Events.Error( error.ToString() );
				}

				return new VRActiveActionSet_t { ulActionSet = handle };
			} ).ToArray();

			var dh = ETrackedControllerRole.Invalid;
			var error3 = Valve.VR.OpenVR.Input.GetDominantHand( ref dh );
			if ( error3 != EVRInputError.None ) {
				Events.Error( $"Couldnt determine dominant hand: {error3}. Falling back to right hand." );
				dh = ETrackedControllerRole.RightHand;
			}
			DominantHand = dh;

			AreComponentsLoaded = true;
			componentsLoaded?.Invoke();
			componentsLoaded = null;
		}

		static event System.Action componentsLoaded;
		/// <summary>
		/// Call the given functions once all controller components are loaded.
		/// </summary>
		public static void BindComponentsLoaded ( System.Action action ) {
			if ( AreComponentsLoaded ) {
				action();
			}
			else componentsLoaded += action;
		}

		/// <summary>
		/// Retreives a controller component for a given name declared in the manifest via <see cref="SetManifest(Manifest)"/>.
		/// The generic type should be one of <see cref="ControllerButton"/>, <see cref="ControllerVector"/>, <see cref="Controller2DVector"/>, <see cref="Controller3DVector"/> or <see cref="ControllerHaptic"/>.
		/// Additionaly if a controller is specified, the controller component will only receive inputs from that controller.
		/// </summary>
		public static T GetControllerComponent<T> ( object name, Controller controller = null ) where T : ControllerComponent {
			controller ??= nullController;
			if ( components.TryGetValue( name, out var cat ) ) {
				if ( cat.TryGetValue( controller, out var comp ) ) {
					return comp as T;
				}
				else {
					comp = cat[ nullController ].CopyWithRestriction( controller.Handle );
					cat.Add( controller, comp );
					return comp as T;
				}
			}
			else return null;
		}

		/// <summary>
		/// Submits the image for an eye. Can only be called after <see cref="UpdateDraw(double)"/>
		/// </summary>
		public static void SubmitFrame ( EVREye eye, Texture_t texture ) {
			VRTextureBounds_t bounds = new VRTextureBounds_t { uMin = 0, uMax = 1, vMin = 0, vMax = 1 };
			var error = Valve.VR.OpenVR.Compositor.Submit( eye, ref texture, ref bounds, EVRSubmitFlags.Submit_Default );
			if ( error != EVRCompositorError.None ) {
				Events.Error( $"Frame submit errors: {eye} : {error}" );
			}
		}
	}

	[Flags]
	public enum VrState {
		NotInitialized = 1,
		OK = 2,
		HeadsetNotDetected = 4,
		UnknownError = 8
	}

	public static class EVRInitErrorExtensions {
		private static Dictionary<EVRInitError, string> descriptions = new() {
			[ EVRInitError.Init_HmdNotFound ] = "Headset not found. This can be a USB issue, or your VR rig might just not be turned on.",
			[ EVRInitError.Init_HmdNotFoundPresenceFailed ] = "Headset not found. This can be a USB issue, or your VR rig might just not be turned on."
		};

		public static string GetReadableDescription ( this EVRInitError error ) {
			if ( descriptions.ContainsKey( error ) ) return $"{descriptions[ error ]} ({error})";
			else return $"{error}";
		}
	}

	public class VrInput {
		public readonly Headset Headset = new();
		public readonly Dictionary<int, Controller> Controllers = new();
	}

	public class Headset {
		public Vector3 Position;
		public Quaternion Rotation;
	}
}
