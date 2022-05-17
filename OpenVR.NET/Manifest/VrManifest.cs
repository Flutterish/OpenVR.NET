using System.Text.Json.Serialization;

namespace OpenVR.NET.Manifest;

/// <summary>
/// Application manifest used to "install" an app into OpenVR, so it can be launched
/// from within VR, have a custom loading icon and a localizable description.
/// </summary>
public class VrManifest {
	/// <summary>
	/// A unique name of the app.
	/// </summary>
	[JsonPropertyName( "app_key" )]
	public string? AppKey;

	[JsonPropertyName( "launch_type" )]
	public readonly string LaunchType = "binary";

	/// <summary>
	/// Path to the executable file compatible with Windows
	/// </summary>
	[JsonPropertyName( "binary_path_windows" )]
	public string? WindowsPath;
	/// <summary>
	/// The startup arguments to pass when launching from OpenVR
	/// </summary>
	[JsonPropertyName( "arguments" )]
	public string? WindowsArguments;

	/// <summary>
	/// Path to the executable file compatible with OSX
	/// </summary>
	[JsonPropertyName( "binary_path_osx" )]
	public string? OSXPath;
	/// <summary>
	/// The startup arguments to pass when launching from OpenVR
	/// </summary>
	[JsonPropertyName( "arguments_osx" )]
	public string? OSXArguments;

	/// <summary>
	/// Path to the executable file compatible with Linux
	/// </summary>
	[JsonPropertyName( "binary_path_linux" )]
	public string? LinuxPath;
	/// <summary>
	/// The startup arguments to pass when launching from OpenVR
	/// </summary>
	[JsonPropertyName( "arguments_linux" )]
	public string? LinuxArguments;

	/// <summary>
	/// Path to the app icon. Can be a file path or a URL.
	/// The image should be at least 460x215, or bigger with the same proportions
	/// </summary>
	[JsonPropertyName( "image_path" )]
	public string? Icon;

	[JsonPropertyName( "is_dashboard_overlay" )]
	public bool IsDashboardOverlay = false;

	[JsonPropertyName( "action_manifest_path" )]
	public string? ActionManifestPath;

	/// <summary>
	/// Localized app name-description pairs. The keys should be lower_snake_case 
	/// ISO language codes, for example "en_us"
	/// </summary>
	[JsonPropertyName( "strings" )]
	public Dictionary<string, NameDescription>? LocalizedNames;
}

public struct NameDescription {
	[JsonPropertyName( "name" )]
	public string Name;
	[JsonPropertyName( "description" )]
	public string Description;

	public static implicit operator NameDescription ( (string name, string description) tuple ) => new() {
		Name = tuple.name,
		Description = tuple.description
	};
}