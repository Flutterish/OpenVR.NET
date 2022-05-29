using OpenVR.NET.Devices;
using VisualTests.Graphics;
using VisualTests.Vertices;

namespace VisualTests.Scene;

public class VrModel {
	public VrDevice Device;
	public ComponentModel Component;
	public Mesh<TexturedVertex> Mesh;
	public Transform Transform = new();
	public Transform ParentTransform = new();
	public Texture? Texture;
	public bool IsVisible = true;

	public VrModel ( VrDevice device, ComponentModel component, Mesh<TexturedVertex> mesh ) {
		Device = device;
		Component = component;
		Mesh = mesh;
		Transform.Parent = ParentTransform;
	}

	public void Update () {
		var pos = Device.RenderPosition;
		var rot = Device.RenderRotation;
		ParentTransform.Position = new( pos.X, pos.Y, pos.Z );
		ParentTransform.Rotation = new( rot.X, rot.Y, rot.Z, rot.W );

		var maybeState = (Device as Controller)?.GetComponentState( Component );
		if ( maybeState is not Controller.ComponentState state )
			return;

		Transform.Position = new( state.Position.X, state.Position.Y, state.Position.Z );
		Transform.Rotation = new( state.Rotation.X, state.Rotation.Y, state.Rotation.Z, state.Rotation.W );

		IsVisible = state.Properties.HasFlag( Valve.VR.EVRComponentProperty.IsVisible );
	}
}
