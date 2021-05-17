using System.Diagnostics.CodeAnalysis;

namespace OpenVR.NET.Manifests {
	// TODO load from bindings folder
	public class DefaultBinding {
		[MaybeNull, NotNull]
		public string ControllerType { get; init; }
		[MaybeNull, NotNull]
		public string Path { get; init; }
	}
}
