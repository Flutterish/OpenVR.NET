namespace OpenVR.NET;

[Flags]
public enum VrState {
	NotInitialized = 1,
	OK = 2,
	HeadsetNotDetected = 4,
	UnknownError = 8
}
