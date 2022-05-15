using OpenVR.NET.Devices;
using OpenVR.NET.Manifest;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Valve.VR;

namespace OpenVR.NET;

/// <summary>
/// A multithreaded, object oriented wrapper around OpenVR.
/// Left handed coordinate system - Y is up, X is right, Z is forward
/// </summary>
public class VR {
	public VrState State { get; private set; } = VrState.NotInitialized;
	public readonly VrEvents Events = new();

	/// <summary>
	/// Tries to initialize OpenVR. It is not required to set the manifest at this point.
	/// This call is blocking and might take several seconds to complete.
	/// </summary>
	public bool TryStart ( EVRApplicationType appType = EVRApplicationType.VRApplication_Scene ) {
		if ( State.HasFlag( VrState.NotInitialized ) ) {
			EVRInitError error = EVRInitError.None;
			CVR = Valve.VR.OpenVR.Init( ref error, appType );
			if ( error is EVRInitError.None ) {
				State = VrState.OK;
				drawContext = new( this );
				Events.Log( "OpenVR initialized succesfuly", EventType.InitializationSuccess, VrState.OK );
			}
			else {
				Events.Log( $"OpenVR could not be initialized", EventType.InitializationError, error );
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Exits VR. You need to make sure <see cref="UpdateDraw"/>, <see cref="UpdateInput"/> nor <see cref="Update"/> are running
	/// and you're not loading any models before making this call. Please be aware that if you do not terminate the running process,
	/// the user will not return to the home scene, but will be left in the vr limbo until they launch another program (it will not terminate yours)
	/// </summary>
	public void Exit () {
		Valve.VR.OpenVR.Shutdown();
	}

	/// <summary>
	/// When the <see cref="EVREventType.VREvent_Quit"/> event is received, call this to extend time
	/// before OpenVR termintes the process
	/// </summary>
	public void GracefullyExit () {
		CVR.AcknowledgeQuit_Exiting();
	}

	/// <summary>
	/// Installs the app/updates the vrmanifest file. If a path is provided,
	/// the .vrmanifest file will be saved there, otherwise it will be saved to the current directory.
	/// Returns the absolute path to the .vrmanifest file, or <see langword="null"/> if the installation failed.
	/// This has to be called after initializing vr.
	/// Set <paramref name="development"/> to <see langword="true"/> for quick iteration with the manifest file,
	/// otherwise it might take a restart of OpenVR or the OS for the changes to apply
	/// </summary>
	public string? InstallApp ( VrManifest manifest, string? path = null, bool development = false ) {
		var json = JsonSerializer.Serialize( new {
			source = "builtin",
			applications = new VrManifest[] { manifest }
		}, new JsonSerializerOptions {
			IncludeFields = true,
			WriteIndented = true,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		} );

		if ( path is null )
			path = ".vrmanifest";
		else if ( !path.EndsWith( ".vrmanifest" ) )
			path += ".vrmanifest";

		path = Path.Combine( Directory.GetCurrentDirectory(), path );
		File.WriteAllText( path, json );

		Valve.VR.OpenVR.Applications.RemoveApplicationManifest( path );
		var error = Valve.VR.OpenVR.Applications.AddApplicationManifest( path, development );
		if ( error != EVRApplicationError.None ) {
			return null;
		}

		return path;
	}

	/// <summary>
	/// Removes a .vrmanifest file from OpenVR, effectively uninstalling the app from it.
	/// You should use this when updating the vrmanifests location.
	/// This has to be called after initializing vr.
	/// </summary>
	public bool UninstallApp ( string vrmanifestPath ) {
		var error = Valve.VR.OpenVR.Applications.RemoveApplicationManifest( vrmanifestPath );
		return error is EVRApplicationError.None;
	}

	#region Draw Thread
	DrawContext drawContext = null!;
	/// <summary>
	/// Returns the openvr CVR System, if initialized.
	/// This is useful for implementing not yet available features of OpenVR.NET.
	/// If you find yourself using unimplemented features, please open an issue
	/// </summary>
	public CVRSystem CVR { get; private set; } = null!;

	/// <summary>
	/// Allows drawing to the headset. Should be called on the draw thread. 
	/// Might block to synchronize with headset refresh rate, depending on headset drivers.
	/// It will also update the player pose, so eyes are positioned correctly.
	/// While not critical, you might want to want to synchronise this pose update with the update thread.
	/// Will return <see langword="null"/> until initialized.
	/// </summary>
	public IVRDrawContext? UpdateDraw ( ETrackingUniverseOrigin origin = ETrackingUniverseOrigin.TrackingUniverseStanding ) {
		if ( State.HasFlag( VrState.OK ) ) {
			pollPoses( origin );
			return drawContext;
		}
		else return null;
	}

	ETrackingUniverseOrigin origin = (ETrackingUniverseOrigin)( -1 );
	TrackedDevicePose_t[] renderPoses = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
	TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
	Dictionary<int, (VrDevice device, VrDevice.Owner owner)> trackedDeviceOwners = new();
	HashSet<VrDevice> activeDevices = new();
	// although in theory this should be on the update thread,
	// this updates once per draw frame and is required to be called to allow for drawing
	void pollPoses ( ETrackingUniverseOrigin origin ) {
		if ( origin != this.origin ) {
			this.origin = origin;
			Valve.VR.OpenVR.Compositor.SetTrackingSpace( origin );
		}

		var error = Valve.VR.OpenVR.Compositor.WaitGetPoses( renderPoses, gamePoses );
		if ( error != EVRCompositorError.None ) {
			Events.Log( $"Player pose could not be retreived", EventType.CoundntFetchPlayerPose, error );
			return;
		}

		for ( int i = 0; i < renderPoses.Length; i++ ) {
			var type = CVR.GetTrackedDeviceClass( (uint)i );
			if ( type is ETrackedDeviceClass.Invalid )
				continue;

			ref var pose = ref renderPoses[i];

			VrDevice device;
			VrDevice.Owner owner;
			if ( !trackedDeviceOwners.TryGetValue( i, out var data ) ) {
				device = type switch {
					ETrackedDeviceClass.HMD => new Headset( this, i, out owner ),
					ETrackedDeviceClass.Controller => new Controller( this, i, out owner ),
					ETrackedDeviceClass.GenericTracker => new Tracker( this, i, out owner ),
					ETrackedDeviceClass.TrackingReference => new TrackingReference( this, i, out owner ),
					ETrackedDeviceClass.DisplayRedirect => new DisplayRedirect( this, i, out owner ),
					_ => new VrDevice( this, i, out owner )
				};

				trackedDeviceOwners.Add( i, (device, owner) );
				updateScheduler.Enqueue( () => {
					trackedDevices.Add( device );
					DeviceDetected?.Invoke( device );
				} );
			}
			else {
				(device, owner) = data;
			}

			if ( pose.bDeviceIsConnected ) {
				if ( !activeDevices.Contains( device ) ) {
					activeDevices.Add( device );
					if ( device is Controller controller ) {
						inputScheduler.Enqueue( () => {
							updateableInputDevices.Add( controller );
						} );
					}
					updateScheduler.Enqueue( () => {
						owner.IsEnabled = true;
					} );
				}
			}
			else {
				if ( activeDevices.Contains( device ) ) {
					activeDevices.Remove( device );
					if ( device is Controller controller ) {
						inputScheduler.Enqueue( () => {
							updateableInputDevices.Remove( controller );
							controller.OnTurnedOff();
						} );
					}
					updateScheduler.Enqueue( () => {
						owner.IsEnabled = false;
					} );
				}
			}

			if ( pose.bPoseIsValid ) {
				owner.RenderPosition = extractPosition( ref pose.mDeviceToAbsoluteTracking );
				owner.RenderRotation = extractRotation( ref pose.mDeviceToAbsoluteTracking );
				pose = ref gamePoses[i];
				owner.Position = extractPosition( ref pose.mDeviceToAbsoluteTracking );
				owner.Rotation = extractRotation( ref pose.mDeviceToAbsoluteTracking );
				owner.Velocity = new( pose.vVelocity.v0, pose.vVelocity.v1, pose.vVelocity.v2 );
				owner.AngularVelocity = new( pose.vAngularVelocity.v0, pose.vAngularVelocity.v1, pose.vAngularVelocity.v2 );
			}

			if ( pose.eTrackingResult != owner.offThreadTrackingState ) {
				var state = pose.eTrackingResult;
				owner.offThreadTrackingState = state;
				updateScheduler.Enqueue( () => {
					owner.TrackingState = state;
				} );
			}
		}
	}
	#endregion
	#region Input Thread
	ConcurrentQueue<Action> inputScheduler = new();
	HashSet<Controller> updateableInputDevices = new();

	/// <summary>
	/// Updates inputs. Should be called on the input or update thread.
	/// </summary>
	public void UpdateInput () {
		while ( inputScheduler.TryDequeue( out var action ) ) {
			action();
		}

		if ( !State.HasFlag( VrState.OK ) )
			return;

		foreach ( var i in updateableInputDevices ) {
			i.UpdateInput();
		}
	}
	#endregion
	#region Update Thread
	ConcurrentQueue<Action> updateScheduler = new();
	bool isPasued = false;

	/// <summary>
	/// Invokes the update thread related events and polls vr events.
	/// </summary>
	public void Update () {
		while ( updateScheduler.TryDequeue( out var action ) ) {
			action();
		}

		if ( !State.HasFlag( VrState.OK ) )
			return;

		if ( Events.AnyOnOpenVrEventHandlers ) {
			VREvent_t e = default;
			while ( CVR.PollNextEvent( ref e, (uint)Marshal.SizeOf<VREvent_t>() ) ) {
				var type = (EVREventType)e.eventType;
				var device = trackedDevices.FirstOrDefault( x => x.DeviceIndex == e.trackedDeviceIndex );
				var age = e.eventAgeSeconds;
				var data = e.data;

				Events.Event( type, device, age, data );
			}
		}

		if ( CVR.ShouldApplicationPause() != isPasued ) {
			isPasued = !isPasued;
			if ( isPasued )
				UserDistracted?.Invoke();
			else
				UserFocused?.Invoke();
		}

		foreach ( var i in trackedDevices ) {
			i.Update();
		}
	}

	/// <summary>
	/// Called on the update thread when the user is doing something else than interacting with the application,
	/// for example changing settings in the overlay
	/// </summary>
	public event Action? UserDistracted;
	/// <summary>
	/// Called on the update thread when the user stops interacting with something else than the application,
	/// for example changing settings in the overlay
	/// </summary>
	public event Action? UserFocused;
	#endregion

	HashSet<VrDevice> trackedDevices = new();
	/// <summary>
	/// All ever detected devices. This is safe to use on the update thread.
	/// </summary>
	public IEnumerable<VrDevice> TrackedDevices => trackedDevices;
	/// <summary>
	/// All enabled devices. This is safe to use on the update thread.
	/// </summary>
	public IEnumerable<VrDevice> ActiveDevices => trackedDevices.Where( x => x.IsEnabled );
	/// <summary> 
	/// Invoked when a new device is detected. Devices can never become "undetected". This is safe to use on the update thread.
	/// </summary>
	public event Action<VrDevice>? DeviceDetected;

	public bool IsHeadsetPresent => Valve.VR.OpenVR.IsHmdPresent();
	[MemberNotNullWhen( true, nameof( OpenVrRuntimePath ) )]
	public bool IsOpenVrRuntimeInstalled => Valve.VR.OpenVR.IsRuntimeInstalled();
	public string? OpenVrRuntimePath => Valve.VR.OpenVR.RuntimePath();
	public string? OpenVrRuntimeVersion => CVR.GetRuntimeVersion();

	Vector3 extractPosition ( ref HmdMatrix34_t mat ) {
		return new Vector3( mat.m3, mat.m7, -mat.m11 );
	}
	Quaternion extractRotation ( ref HmdMatrix34_t mat ) {
		Quaternion q = default;
		q.W = MathF.Sqrt( MathF.Max( 0, 1 + mat.m0 + mat.m5 + mat.m10 ) ) / 2;
		q.X = MathF.Sqrt( MathF.Max( 0, 1 + mat.m0 - mat.m5 - mat.m10 ) ) / 2;
		q.Y = MathF.Sqrt( MathF.Max( 0, 1 - mat.m0 + mat.m5 - mat.m10 ) ) / 2;
		q.Z = MathF.Sqrt( MathF.Max( 0, 1 - mat.m0 - mat.m5 + mat.m10 ) ) / 2;
		q.X = MathF.CopySign( q.X, mat.m9 - mat.m6 );
		q.Y = MathF.CopySign( q.Y, mat.m2 - mat.m8 );
		q.Z = MathF.CopySign( q.Z, mat.m1 - mat.m4 );

		var scale = 1 / q.LengthSquared();
		return new Quaternion( q.X * -scale, q.Y * -scale, q.Z * -scale, q.W * scale );
	}
}
