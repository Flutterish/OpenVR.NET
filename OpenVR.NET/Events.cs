using System;

namespace OpenVR.NET {
	public static class Events {
		public static void Log ( string messgae ) { OnLog?.Invoke( messgae ); OnAnyEvent?.Invoke( messgae, null ); }
		public static void Message ( string messgae ) { OnMessage?.Invoke( messgae ); OnAnyEvent?.Invoke( messgae, null ); }
		public static void Warning ( string messgae ) { OnWarning?.Invoke( messgae ); OnAnyEvent?.Invoke( messgae, null ); }
		public static void Error ( string messgae ) { OnError?.Invoke( messgae ); OnAnyEvent?.Invoke( messgae, null ); }
		public static void Exception ( Exception e, string messgae ) { OnException?.Invoke( messgae, e ); OnAnyEvent?.Invoke( messgae, e ); }

		public static event Action<string>? OnLog;
		public static event Action<string>? OnMessage;
		public static event Action<string>? OnWarning;
		public static event Action<string>? OnError;
		public static event Action<string,Exception?>? OnException;

		public static event Action<string,Exception?>? OnAnyEvent;
	}
}
