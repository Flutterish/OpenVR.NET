using OpenVR.NET.Devices;
using OpenVR.NET.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using Valve.VR;

namespace OpenVR.NET.Manifest;

/// <summary>
/// A manifest which defines all possible actions the user can perform in an appliaction.
/// Using this manifest means you can no longer use <seealso cref="Devices.Controller.ILegacyAction"/>.
/// The defaullt implementation is <see cref="ActionManifest{Tcategory, Taction}"/>
/// </summary>
public interface IActionManifest {
	string ToJson ();
	IEnumerable<IActionSet> ActionSets { get; }
	IEnumerable<IAction> ActionsForSet ( IActionSet set );
}

/// <summary>
/// A manifest which defines all possible actions the user can perform in an appliaction.
/// Using this manifest means you can no longer use <seealso cref="Devices.Controller.ILegacyAction"/>
/// </summary>
public class ActionManifest<Tcategory, Taction> : IActionManifest
	where Tcategory : struct, Enum
	where Taction : struct, Enum {

	public List<ActionSet<Tcategory, Taction>>? ActionSets;
	public List<Action<Tcategory, Taction>>? Actions;
	public List<DefaultBinding>? DefaultBindings;
	public uint? Version;
	public uint? RequiredVersion;
	public bool? SupportsDominantHandSetting;

	IEnumerable<IActionSet> IActionManifest.ActionSets
		=> ActionSets is null ? Array.Empty<IActionSet>() : ActionSets;

	public IEnumerable<IAction> ActionsForSet ( IActionSet set ) {
		if ( Actions is null )
			return Array.Empty<IAction>();

		var cat = ( (ActionSet<Tcategory, Taction>)set ).Name;
		return Actions.Where( x => x.Category.Equals( cat ) );
	}

	static Dictionary<ActionType, string> typeNames = new() {
		[ActionType.Boolean] = "boolean",
		[ActionType.Scalar] = "vector1",
		[ActionType.Vector2] = "vector2",
		[ActionType.Vector3] = "vector3",
		[ActionType.Vibration] = "vibration",
		[ActionType.Pose] = "pose",
		[ActionType.LeftHandSkeleton] = "skeleton",
		[ActionType.RightHandSkeleton] = "skeleton"
	};

	public string ToJson () {
		object manifest = new {
			default_bindings = DefaultBindings,
			supports_dominant_hand_setting = SupportsDominantHandSetting,
			actions = Actions?.Select( x => new {
				name = x.Path,
				requirement = x.Requirement is Requirement.Suggested ? null : x.Requirement.ToString().ToLower(),
				type = typeNames[x.Type],
				skeleton = x.Type is ActionType.LeftHandSkeleton
					? "/skeleton/hand/left"
					: x.Type is ActionType.RightHandSkeleton
					? "/skeleton/hand/right"
					: null
			} ).ToArray(),
			action_sets = ActionSets?.Select( x => new {
				name = x.Path,
				usage = x.Type.ToString().ToLower()
			} ).ToArray(),
			localization = ( ActionSets?.AsEnumerable() ?? Array.Empty<ActionSet<Tcategory, Taction>>() )
				.Where( x => x.LocalizedNames != null ).SelectMany( l => l.LocalizedNames!.Select( x => (
					tag: x.Key,
					key: l.Path,
					value: x.Value
				) )
			).Concat( ( Actions?.AsEnumerable() ?? Array.Empty<Action<Tcategory, Taction>>() )
				.Where( x => x.LocalizedNames != null ).SelectMany( l => l.LocalizedNames!.Select( x => (
					tag: x.Key,
					key: l.Path,
					value: x.Value
				) ) )
			).GroupBy( x => x.tag ).Select( x =>
				x.Append( (tag: x.Key, key: "language_tag", value: x.Key) ).ToDictionary(
					k => k.key,
					e => e.value
				)
			).ToArray(),
			version = Version,
			minimum_required_version = RequiredVersion
		};

		return JsonSerializer.Serialize( manifest, new JsonSerializerOptions {
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = true,
			IncludeFields = true
		} );
	}
}

public struct DefaultBinding {
	/// <summary>
	/// Controller type such as "vive_controller", "oculus_touch" or "knuckles".
	/// This type appears in the bindings file when you export bindings for a particular controller.
	/// </summary>
	[JsonPropertyName( "controller_type" )]
	public string ControllerType;

	/// <summary>
	/// Relative path, or a URL to the bindings file
	/// </summary>
	[JsonPropertyName( "binding_url" )]
	public string Path;
}

public enum ActionType {
	Boolean,
	Scalar,
	Vector2,
	Vector3,
	Vibration,
	Pose,
	LeftHandSkeleton,
	RightHandSkeleton
}

public enum Requirement {
	Mandatory,
	Suggested,
	Optional
}

public enum ActionSetType {
	LeftRight,
	Single,
	Hidden
}

public interface IActionSet {
	string Path { get; }
	Enum Name { get; }
}

public class ActionSet<Tcategory, Taction> : IActionSet
	where Tcategory : struct, Enum
	where Taction : struct, Enum {

	public Tcategory Name;
	public ActionSetType Type;
	/// <summary>
	/// Localized names for this action set. The keys should be lower_snake_case 
	/// ISO language codes, for example "en_us"
	/// </summary>
	public Dictionary<string, string>? LocalizedNames;

	public string Path => $"/actions/{Name}";
	Enum IActionSet.Name => Name;
}

public interface IAction {
	string Path { get; }
	Enum Name { get; }

	Input.Action CreateAction ( VR vr, Controller? device );
}

public class Action<Tcategory, Taction> : IAction
	where Tcategory : struct, Enum
	where Taction : struct, Enum {

	public Taction Name;
	public Tcategory Category;
	public Requirement Requirement = Requirement.Suggested;
	public ActionType Type;

	/// <summary>
	/// Localized names for this action. The keys should be lower_snake_case 
	/// ISO language codes, for example "en_us"
	/// </summary>
	public Dictionary<string, string>? LocalizedNames;

	public string Path => $"/actions/{Category}/{( Type is ActionType.Vibration ? "out" : "in" )}/{Name}";
	Enum IAction.Name => Name;

	public Input.Action CreateAction ( VR vr, Controller? device ) {
		ulong handle = 0;
		var error = Valve.VR.OpenVR.Input.GetActionHandle( Path, ref handle );
		if ( error != EVRInputError.None ) {
			vr.Events.Log( $"Could not get handle for action {Category}/{Name}", EventType.CoundntFetchActionHandle, error );
		}

		var deviceHandle = device?.Handle ?? Valve.VR.OpenVR.k_ulInvalidActionHandle;

		return Type switch {
			ActionType.Boolean => new BooleanAction { SourceHandle = handle, DeviceHandle = deviceHandle },
			ActionType.Scalar => new ScalarAction { SourceHandle = handle, DeviceHandle = deviceHandle },
			ActionType.Vector2 => new Vector2Action { SourceHandle = handle, DeviceHandle = deviceHandle },
			ActionType.Vector3 => new Vector3Action { SourceHandle = handle, DeviceHandle = deviceHandle },
			ActionType.Vibration => new HapticAction { SourceHandle = handle, DeviceHandle = deviceHandle },
			ActionType.Pose => new PoseAction { SourceHandle = handle, DeviceHandle = deviceHandle, VR = vr },
			ActionType.LeftHandSkeleton => new HandSkeletonAction { SourceHandle = handle, DeviceHandle = deviceHandle },
			ActionType.RightHandSkeleton => new HandSkeletonAction { SourceHandle = handle, DeviceHandle = deviceHandle },
			_ => throw new InvalidOperationException( $"No action exists for {Type}" )
		};
	}
}