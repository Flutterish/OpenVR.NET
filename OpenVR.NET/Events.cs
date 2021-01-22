using System;

namespace OpenVR.NET {
	public static class Events {
		public static void Log ( string messgae ) { }
		public static void Message ( string messgae ) { }
		public static void Warning ( string messgae ) { }
		public static void Error ( string messgae ) { }
		public static void Exception ( Exception e, string messgae ) { }
	}
}
