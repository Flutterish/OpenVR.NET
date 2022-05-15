using System.Numerics;
using Valve.VR;

namespace OpenVR.NET;

public interface IVRDrawContext {
	Vector2 Resolution { get; }
	void SubmitFrame ( EVREye eye, Texture_t texture, EVRSubmitFlags flags = EVRSubmitFlags.Submit_Default );
}

class DrawContext : IVRDrawContext {
	VR VR;
	public DrawContext ( VR vr ) {
		VR = vr;
		uint w = 0, h = 0;
		VR.CVR.GetRecommendedRenderTargetSize( ref w, ref h );
		Resolution = new Vector2( w, h );
	}

	/// <summary>
	/// Submits the image for an eye. Can only be called after <see cref="UpdateDraw(double)"/>
	/// </summary>
	public void SubmitFrame ( EVREye eye, Texture_t texture, EVRSubmitFlags flags = EVRSubmitFlags.Submit_Default ) {
		VRTextureBounds_t bounds = new VRTextureBounds_t { uMin = 0, uMax = 1, vMin = 0, vMax = 1 };
		var error = Valve.VR.OpenVR.Compositor.Submit( eye, ref texture, ref bounds, flags );
		if ( error != EVRCompositorError.None ) {
			VR.Events.Log( $"Could not sumbit frame for {( eye is EVREye.Eye_Left ? "left" : "right" )} eye", EventType.FrameSubmitError, (error, eye) );
		}
	}

	public Vector2 Resolution { get; }
}
