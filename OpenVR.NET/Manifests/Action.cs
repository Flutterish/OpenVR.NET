using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OpenVR.NET.Manifests {
	/// <summary>
	/// A controller binding. Contains what type of input it uses and what enum name it has.
	/// </summary>
	public class Action<T> : Action where T : Enum {
		[MaybeNull, NotNull]
		public T Name { get; init; }
		public override string GetReadableName () => Name.ToString();
		public override ControllerComponent CreateComponent ( ulong handle ) {
			switch ( Type ) {
				case ActionType.Boolean:
					return new ControllerButton { Handle = handle, Name = Name };
				case ActionType.Vector1:
					return new ControllerVector { Handle = handle, Name = Name };
				case ActionType.Vector2:
					return new Controller2DVector { Handle = handle, Name = Name };
				case ActionType.Vector3:
					return new Controller3DVector { Handle = handle, Name = Name };
				case ActionType.Vibration:
					return new ControllerHaptic { Handle = handle, Name = Name };
				case ActionType.Skeleton:
					throw new InvalidOperationException( $"No controller component exists for {Type}" );
				case ActionType.Pose:
					throw new InvalidOperationException( $"No controller component exists for {Type}" );
				default:
					throw new InvalidOperationException( $"No controller component exists for {Type}" );
			}
		}
	}

	/// <summary>
	/// Base type of <see cref="Action{T}"/> as Enums are not covariant. Don't use this.
	/// </summary>
	public abstract class Action {
		public abstract string GetReadableName ();
		public ActionType Type { get; set; }
		public Requirement Requirement { get; set; }
		/// <summary>
		/// Locale names. The key should follow http://www.lingoes.net/en/translator/langcode.htm (but lowercase snake case).
		/// </summary>
		public Dictionary<string, string> Localizations = new();

		/// <summary>
		/// The full action name. This is usually set by <see cref="VrManager"/>
		/// </summary>
		public string FullPath { get; set; } = string.Empty;

		public abstract ControllerComponent CreateComponent ( ulong handle );
	}
}
