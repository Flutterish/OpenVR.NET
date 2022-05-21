using OpenVR.NET.Devices;
using VisualTests.Graphics;
using VisualTests.Vertices;

namespace VisualTests.Scene;

public class VrModel {
	public Controller Device;
	public ComponentModel Component;
	public Mesh<TexturedVertex> Mesh;
	public Transform Transform = new();
	public Transform ParentTransform = new();
	public Texture? Texture;
	public bool IsVisible;

	public VrModel ( Controller device, ComponentModel component, Mesh<TexturedVertex> mesh ) {
		Device = device;
		Component = component;
		Mesh = mesh;
		Transform.Parent = ParentTransform;
	}

	public void Update () {
		var maybeState = Device.GetComponentState( Component );
		if ( maybeState is not Controller.ComponentState state )
			return;

		ParentTransform.Position = new( Device.Position.X, Device.Position.Y, Device.Position.Z );
		ParentTransform.Rotation = new( Device.Rotation.X, Device.Rotation.Y, Device.Rotation.Z, Device.Rotation.W );

		Transform.Position = new( state.GlobalPosition.X, state.GlobalPosition.Y, state.GlobalPosition.Z );
		Transform.Rotation = new( state.GlobalRotation.X, state.GlobalRotation.Y, state.GlobalRotation.Z, state.GlobalRotation.W );

		IsVisible = state.Properties.HasFlag( Valve.VR.EVRComponentProperty.IsVisible );
	}
}
