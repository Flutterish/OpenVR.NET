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
		ParentTransform.Position = new( Device.Position.X, Device.Position.Y, Device.Position.Z );
		ParentTransform.Rotation = new( Device.Rotation.X, Device.Rotation.Y, Device.Rotation.Z, Device.Rotation.W );

		var maybeState = (Device as Controller)?.GetComponentState( Component );
		if ( maybeState is not Controller.ComponentState state )
			return;

		Transform.Position = new( state.Position.X, state.Position.Y, state.Position.Z );
		Transform.Rotation = new( state.Rotation.X, state.Rotation.Y, state.Rotation.Z, state.Rotation.W );

		IsVisible = state.Properties.HasFlag( Valve.VR.EVRComponentProperty.IsVisible );
	}
}
