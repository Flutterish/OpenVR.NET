namespace OpenVR.NET.Manifests {
	public enum Requirement {
		/// <summary>
		/// The user will be warned if this is not bound
		/// </summary>
		Suggested,
		/// <summary>
		/// The bindings cannot be saved witohut this bound
		/// </summary>
		Mandatory,
		/// <summary>
		/// No warning are produced if this is not bound
		/// </summary>
		Optional
	}

	public enum ActionType {
		/// <summary>
		/// Boolean actions are used for triggers and buttons
		/// </summary>
		Boolean,
		/// <summary>
		/// Vecotr1 actions are read from triggers, 1D trackpads and joysticks 
		/// </summary>
		Vector1,
		/// <summary>
		/// Vector2 actions are read from trackpads and joysticks
		/// </summary>
		Vector2,
		Vector3,
		/// <summary>
		/// Vibration is used to activate haptics
		/// </summary>
		Vibration,
		Pose,
		Skeleton
	}

	public enum ActionGroupType {
		/// <summary>
		/// These actions can be bound on each controller separately
		/// </summary>
		LeftRight,
		/// <summary>
		/// These actions can be bound only on one controller
		/// </summary>
		Single,
		/// <summary>
		/// These actions are hidden
		/// </summary>
		Hidden
	}
}
