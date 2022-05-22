using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using Valve.VR;

namespace OpenVR.NET;

/// <summary>
/// Playfeld bounds and tracking safety
/// </summary>
public interface IChaperone {
	ChaperoneCalibrationState State { get; }
	event Action<ChaperoneCalibrationState>? StateChanged;

	/// <summary>
	/// Playfield size in meters
	/// </summary>
	Vector2? PlayfieldSize { get; }
	/// <summary>
	/// Playfield bounds in meters, where (0,0) is the playfield centre
	/// </summary>
	(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)? PlayfieldBounds { get; }
	/// <summary>
	/// Hints the scene ambience to the system to better match the system-rendered parts of the scene 
	/// such as playfield bounds or the freeze-limbo
	/// </summary>
	void SetSceneColourHint ( Rgba32 colour );
	/// <summary>
	/// Whether the system is rendering playfield bounds
	/// </summary>
	bool AreBoundsVisible { get; }
	/// <summary>
	/// Force the system to draw playfield bounds
	/// </summary>
	bool ForceBoundsVisible { get; set; }
}

class Chaperone : IChaperone {
	protected VR VR;
	public Chaperone ( VR vr ) {
		VR = vr;
	}

	/// <summary>
	/// Update on the update thread.
	/// </summary>
	public void Update () {
		State = Valve.VR.OpenVR.Chaperone.GetCalibrationState();
	}

	ChaperoneCalibrationState state = 0;
	public ChaperoneCalibrationState State {
		get => state;
		private set {
			if ( value == state )
				return;

			state = value;
			StateChanged?.Invoke( value );
		}
	}

	public event Action<ChaperoneCalibrationState>? StateChanged;

	public Vector2? PlayfieldSize {
		get {
			float x = 0, z = 0;
			if ( Valve.VR.OpenVR.Chaperone.GetPlayAreaSize( ref x, ref z ) )
				return new( x, z );
			return null;
		}
	}

	public (Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight)? PlayfieldBounds {
		get {
			HmdQuad_t quad = default;
			if ( Valve.VR.OpenVR.Chaperone.GetPlayAreaRect( ref quad ) ) {
				return (
					new( quad.vCorners1.v0, -quad.vCorners1.v2 ),
					new( quad.vCorners0.v0, -quad.vCorners0.v2 ),
					new( quad.vCorners2.v0, -quad.vCorners2.v2 ),
					new( quad.vCorners3.v0, -quad.vCorners3.v2 )
				);
			}
			return null;
		}
	}

	public void SetSceneColourHint ( Rgba32 colour ) {
		var v = colour.ToScaledVector4();
		Valve.VR.OpenVR.Chaperone.SetSceneColor( new() { r = v.X, g = v.Y, b = v.Z, a = v.W } );
	}

	public bool AreBoundsVisible => Valve.VR.OpenVR.Chaperone.AreBoundsVisible();

	bool forceBoundsVisible = false;
	public bool ForceBoundsVisible {
		get => forceBoundsVisible;
		set {
			Valve.VR.OpenVR.Chaperone.ForceBoundsVisible( value );
			forceBoundsVisible = value;
		}
	}
}
