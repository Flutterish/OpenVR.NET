# OpenVR.NET
 
OpenVR.NET is a set of C# bindings to openxr via the Valve openvr implementation. It encapsulates openvr in an object-oriented and event driven way. This is a library, not a framework, if you wish to incorporate it into your project, you will need to call the "heartbeat" methods of OpenVR.NET yourself.

This project was originally a part of osu!xr (a VR port of osu!lazer), but I deemed it useful for other creators, and as such it was split into its own thing.

OpenVR.NET currently allows you to render to VR, set the VR manifest at runtime and handle inputV2. Skeleton pose is not implemented yet.

# ⚠️ WARNING ⚠️
OpenVR.NET currently has problems with threading and sometimes uses locks. This will be fixed in the future.