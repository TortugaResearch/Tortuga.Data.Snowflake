using Tortuga.Data.Snowflake.Core.Messages;

namespace Tortuga.Data.Snowflake.Core;

internal class SFEnvironment
{
	static SFEnvironment()
	{
		ClientEnv = new LoginRequestClientEnv()
		{
			application = System.Diagnostics.Process.GetCurrentProcess().ProcessName,
			osVersion = Environment.OSVersion.VersionString,
#if NETFRAMEWORK
			netRuntime = "NETFramework",
			netVersion = "4.7.2",
#else
                netRuntime = "NETCore",
                netVersion ="2.0",
#endif
		};

		DriverVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		DriverName = ".NET";
	}

	internal static string DriverName { get; private set; }
	internal static string DriverVersion { get; private set; }
	internal static LoginRequestClientEnv ClientEnv { get; private set; }
}
