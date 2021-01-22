using System;
using System.Collections.Generic;

namespace OpenVR.NET.Manifests {
	/// <summary>
	/// A group of <see cref="Action{TAction}"/>. Used to set the <see cref="ActionGroupType"/> of child <see cref="Action{T}"/>
	/// </summary>
	public class ActionGroup<TGroup, TAction> : ActionGroup where TAction : Enum where TGroup : Enum {
		public TGroup Name { get; set; }
		public List<Action<TAction>> Actions = new();

		public override string GetReadableName ()
			=> Name.ToString();

		public override IEnumerable<Action> EnumerateActions ()
			=> Actions;
	}

	/// <summary>
	/// Base type of <see cref="ActionGroup{TGroup, TAction}"/> as Enums are not covariant. Don't use this.
	/// </summary>
	public abstract class ActionGroup {
		public ActionGroupType Type { get; set; }
		public abstract string GetReadableName ();
		/// <summary>
		/// The full group name. This is usually set by <see cref="VrManager"/>
		/// </summary>
		public string FullPath;
		public abstract IEnumerable<Action> EnumerateActions ();
		/// <summary>
		/// Locale names. The key should follow http://www.lingoes.net/en/translator/langcode.htm (but lowercase snake case).
		/// </summary>
		public Dictionary<string, string> Localizations = new();
	}
}
