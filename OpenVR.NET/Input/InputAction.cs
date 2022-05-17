using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Valve.VR;
using static OpenVR.NET.Extensions;

namespace OpenVR.NET.Input;

/// <inheritdoc/>
public abstract class InputAction<T> : Action where T : struct {
	private T value;
	public T Value {
		get => value;
		protected set {
			if ( value.Equals( this.value ) )
				return;

			var prev = this.value;
			this.value = value;

			ValueChanged?.Invoke( prev, value );
			ValueUpdated?.Invoke( value );
		}
	}

	public delegate void ValueChangedHandler ( T oldValue, T newValue );
	public event ValueChangedHandler? ValueChanged;
	public event Action<T>? ValueUpdated;
}

public class BooleanAction : InputAction<bool> {
	public override void Update () {
		InputDigitalActionData_t data = default;
		var error = Valve.VR.OpenVR.Input.GetDigitalActionData( SourceHandle, ref data, (uint)Marshal.SizeOf<InputDigitalActionData_t>(), DeviceHandle );
		if ( error is EVRInputError.None && data.bActive )
			Value = data.bState;
	}
}

public class ScalarAction : InputAction<float> {
	public override void Update () {
		InputAnalogActionData_t data = default;
		var error = Valve.VR.OpenVR.Input.GetAnalogActionData( SourceHandle, ref data, (uint)Marshal.SizeOf<InputAnalogActionData_t>(), DeviceHandle );
		if ( error is EVRInputError.None && data.bActive )
			Value = data.x;
	}
}

public class Vector2Action : InputAction<Vector2> {
	public override void Update () {
		InputAnalogActionData_t data = default;
		var error = Valve.VR.OpenVR.Input.GetAnalogActionData( SourceHandle, ref data, (uint)Marshal.SizeOf<InputAnalogActionData_t>(), DeviceHandle );
		if ( error is EVRInputError.None && data.bActive )
			Value = new( data.x, data.y );
	}
}

public class Vector3Action : InputAction<Vector3> {
	public override void Update () {
		InputAnalogActionData_t data = default;
		var error = Valve.VR.OpenVR.Input.GetAnalogActionData( SourceHandle, ref data, (uint)Marshal.SizeOf<InputAnalogActionData_t>(), DeviceHandle );
		if ( error is EVRInputError.None && data.bActive )
			Value = new( data.x, data.y, data.z );
	}
}

public struct PoseInput {
	/// <inheritdoc cref="Devices.VrDevice.Position"/>
	public Vector3 Position;
	/// <inheritdoc cref="Devices.VrDevice.Rotation"/>
	public Quaternion Rotation;
	/// <inheritdoc cref="Devices.VrDevice.Velocity"/>
	public Vector3 Velocity;
	/// <inheritdoc cref="Devices.VrDevice.AngularVelocity"/>
	public Vector3 AngularVelocity;
}
public class PoseAction : Action {
	public VR VR { get; init; } = null!;

	public override void Update () { }

	public PoseInput? FetchData ()
		=> fetchData();

	public PoseInput? FetchDataForPrediction ( float secondsFromNow )
		=> fetchData( predictOffset: secondsFromNow );

	public PoseInput? FetchDataForNextFrame ()
		=> fetchData( predictNextFrame: true );

	PoseInput? fetchData ( bool predictNextFrame = false, float predictOffset = 0 ) {
		InputPoseActionData_t data = default;
		var error = predictNextFrame ? Valve.VR.OpenVR.Input.GetPoseActionDataForNextFrame(
				SourceHandle, VR.TrackingOrigin,
				ref data, (uint)Marshal.SizeOf<InputPoseActionData_t>(),
				DeviceHandle
			)
			: Valve.VR.OpenVR.Input.GetPoseActionDataRelativeToNow(
				SourceHandle, VR.TrackingOrigin,
				predictOffset, ref data, (uint)Marshal.SizeOf<InputPoseActionData_t>(),
				DeviceHandle
			);

		if ( error is EVRInputError.None && data.bActive ) {
			return new() {
				Position = ExtractPosition( ref data.pose.mDeviceToAbsoluteTracking ),
				Rotation = ExtractRotation( ref data.pose.mDeviceToAbsoluteTracking ),
				Velocity = new( data.pose.vVelocity.v0, data.pose.vVelocity.v1, -data.pose.vVelocity.v2 ),
				AngularVelocity = new( data.pose.vAngularVelocity.v0, data.pose.vAngularVelocity.v1, -data.pose.vAngularVelocity.v2 )
			};
		}
		else return null;
	}
}

public struct HandSkeletonSummary {
	public float ThumbCurl;
	public float IndexCurl;
	public float MiddleCurl;
	public float RingCurl;
	public float PinkyCurl;
	public float ThumbIndexSplay;
	public float IndexMiddleSplay;
	public float MiddleRingSplay;
	public float RingPinkySplay;
}
public struct BoneData {
	public Vector4 Position;
	public Quaternion Rotation;
}
/// <summary>
/// <see href="https://github.com/ValveSoftware/openvr/wiki/Hand-Skeleton"/>
/// <see href="https://github.com/ValveSoftware/openvr/wiki/SteamVR-Skeletal-Input"/>
/// </summary>
public class HandSkeletonAction : Action {
	VRBoneTransform_t[] bones = new VRBoneTransform_t[31];
	int[] hierarchy = new int[31];

	public EVRSkeletalTrackingLevel TrackingLevel { get; private set; }
	public override void Update () {
		EVRSkeletalTrackingLevel trackingLevel = default;
		var trackingError = Valve.VR.OpenVR.Input.GetSkeletalTrackingLevel( SourceHandle, ref trackingLevel );
		if ( trackingError is EVRInputError.None )
			TrackingLevel = trackingLevel;
	}

	public int BoneCount { get; private set; } = 31;
	public string GetBoneName ( int index ) {
		StringBuilder sb = new( (int)Valve.VR.OpenVR.k_unMaxBoneNameLength );
		Valve.VR.OpenVR.Input.GetBoneName( SourceHandle, index, sb, Valve.VR.OpenVR.k_unMaxBoneNameLength );
		return sb.ToString();
	}
	public int ParentBoneIndex ( int index )
		=> hierarchy[index];
	public BoneData GetBoneData ( int index ) {
		var data = bones[index];
		return new() {
			Position = new( data.position.v0, data.position.v1, -data.position.v2, data.position.v3 ),
			Rotation = new( data.orientation.x, data.orientation.y, -data.orientation.z, -data.orientation.w )
		};
	}

	public bool FetchData (
		EVRSkeletalTransformSpace transformSpace = EVRSkeletalTransformSpace.Model,
		EVRSkeletalMotionRange motionRange = EVRSkeletalMotionRange.WithController
	) => fetchData( transformSpace, motionRange );

	public bool FetchReferenceData (
		EVRSkeletalReferencePose referencePose,
		EVRSkeletalTransformSpace transformSpace = EVRSkeletalTransformSpace.Model,
		EVRSkeletalMotionRange motionRange = EVRSkeletalMotionRange.WithController
	) => fetchData( transformSpace, motionRange, referencePose );

	bool fetchData (
		EVRSkeletalTransformSpace transformSpace = EVRSkeletalTransformSpace.Model,
		EVRSkeletalMotionRange motionRange = EVRSkeletalMotionRange.WithController,
		EVRSkeletalReferencePose? overridePose = null
	) {
		InputSkeletalActionData_t data = default;
		var error = Valve.VR.OpenVR.Input.GetSkeletalActionData( SourceHandle, ref data, (uint)Marshal.SizeOf<InputSkeletalActionData_t>() );
		if ( error != EVRInputError.None || !data.bActive )
			return false;

		uint boneCount = 0;
		error = Valve.VR.OpenVR.Input.GetBoneCount( SourceHandle, ref boneCount );
		if ( error != EVRInputError.None || !data.bActive )
			return false;
		if ( boneCount > bones.Length ) {
			bones = new VRBoneTransform_t[(int)boneCount];
			hierarchy = new int[(int)boneCount];
		}
		BoneCount = (int)boneCount;

		error = Valve.VR.OpenVR.Input.GetBoneHierarchy( SourceHandle, hierarchy );
		if ( error != EVRInputError.None )
			return false;

		error = overridePose is EVRSkeletalReferencePose pose
			? Valve.VR.OpenVR.Input.GetSkeletalReferenceTransforms( SourceHandle, transformSpace, pose, bones )
			: Valve.VR.OpenVR.Input.GetSkeletalBoneData( SourceHandle, transformSpace, motionRange, bones );
		if ( error != EVRInputError.None )
			return false;

		return true;
	}

	public HandSkeletonSummary? GetSummary ( EVRSummaryType type ) {
		VRSkeletalSummaryData_t data = default;
		var error = Valve.VR.OpenVR.Input.GetSkeletalSummaryData( SourceHandle, type, ref data );

		if ( error != EVRInputError.None )
			return null;

		return new() {
			ThumbCurl = data.flFingerCurl0,
			IndexCurl = data.flFingerCurl1,
			MiddleCurl = data.flFingerCurl2,
			RingCurl = data.flFingerCurl3,
			PinkyCurl = data.flFingerCurl4,
			ThumbIndexSplay = data.flFingerSplay0,
			IndexMiddleSplay = data.flFingerSplay1,
			MiddleRingSplay = data.flFingerSplay2,
			RingPinkySplay = data.flFingerSplay3
		};
	}
}