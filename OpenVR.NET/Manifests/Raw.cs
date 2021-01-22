using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace OpenVR.NET.Manifests {
	// This set of classes is used to translate our manifest into openVR manifest

	internal class RawManifest {
		[JsonPropertyName( "actions" )]
		public List<RawAction> Actions = new();
		[JsonPropertyName( "action_sets" )]
		public List<RawActionSet> ActionSets = new();
		[JsonPropertyName( "localization" )]
		public List<Dictionary<string, string>> Localizations = new();
		[JsonPropertyName( "default_bindings" )]
		public List<RawDefault> DefaultBindings = new();

		public static (RawManifest actionManifest, RawVrManifest vrManifest) Parse ( Manifest source ) {
			RawManifest actionManifest = new();

			foreach ( var group in source.EnumerateGroups() ) {
				var rawGroup = new RawActionSet {
					Name = $"/actions/{group.GetReadableName()}".ToLower(),
					Usage = group.Type.ToString().ToLower()
				};
				group.FullPath = rawGroup.Name;
				actionManifest.ActionSets.Add( rawGroup );
				foreach ( var loc in group.Localizations ) {
					getLocale( loc.Key, actionManifest.Localizations ).Add( rawGroup.Name, loc.Value );
				}
				foreach ( var action in group.EnumerateActions() ) {
					string path = $"{rawGroup.Name}/{( action.Type == ActionType.Vibration ? "out" : "in" )}/{action.GetReadableName()}".ToLower();
					var rawAction = new RawAction {
						Name = path,
						Requirement = action.Requirement.ToString().ToLower(),
						Type = action.Type.ToString().ToLower()
					};
					action.FullPath = path;
					actionManifest.Actions.Add( rawAction );

					foreach ( var loc in action.Localizations ) {
						getLocale( loc.Key, actionManifest.Localizations ).Add( path, loc.Value );
					}
				}
			}
			foreach ( var binding in source.DefaultBindings ) {
				actionManifest.DefaultBindings.Add( new RawDefault {
					Path = binding.Path,
					Type = binding.ControllerType
				} );
			}

			RawVrManifest vrManifest = new();
			var app = new RawVrManifestApp {
				AppKey = source.Name,
				LaunchType = source.LaunchType.ToString().ToLower(),
				Path = source.Path ?? Assembly.GetEntryAssembly().Location,
				IsDashboardOverlay = source.IsDashBoardOverlay,
				ActionManifestPath = $"./{VR.ACTION_MANIFEST_NAME}"
			};
			foreach ( var i in source.Localizations ) {
				app.Strings.Add( i.LanguageTag, new() {
					[ "name" ] = i.Name,
					[ "description" ] = i.Description
				} );
			}
			vrManifest.Apps.Add( app );

			return (actionManifest, vrManifest);
		}

		static Dictionary<string, string> getLocale ( string languageTag, List<Dictionary<string, string>> locales ) {
			Dictionary<string, string> list = locales.FirstOrDefault( x => x[ "language_tag" ] == languageTag );
			if ( list is null ) {
				list = new Dictionary<string, string> { [ "language_tag" ] = languageTag };
				locales.Add( list );
			}
			return list;
		}
	}

	internal class RawAction {
		[JsonPropertyName( "name" )]
		public string Name;
		[JsonPropertyName( "requirement" )]
		public string Requirement;
		[JsonPropertyName( "type" )]
		public string Type;
	}

	internal class RawActionSet {
		[JsonPropertyName( "name" )]
		public string Name;
		[JsonPropertyName( "usage" )]
		public string Usage;
	}

	internal class RawDefault {
		[JsonPropertyName( "controller_type" )]
		public string Type;
		[JsonPropertyName( "binding_url" )]
		public string Path;
	}

	internal class RawVrManifest {
		[JsonPropertyName( "applications" )]
		public List<RawVrManifestApp> Apps = new();
	}

	internal class RawVrManifestApp {
		[JsonPropertyName( "app_key" )]
		public string AppKey;
		[JsonPropertyName( "launch_type" )]
		public string LaunchType;
		[JsonPropertyName( "binary_path_windows" )]
		public string Path;
		[JsonPropertyName( "is_dashboard_overlay" )]
		public bool IsDashboardOverlay;
		[JsonPropertyName( "action_manifest_path" )]
		public string ActionManifestPath;
		[JsonPropertyName( "strings" )]
		public Dictionary<string, Dictionary<string, string>> Strings = new();
	}
}
