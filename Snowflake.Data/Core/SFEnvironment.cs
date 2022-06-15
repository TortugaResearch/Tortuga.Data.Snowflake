using Tortuga.Data.Snowflake.Core.Messages;

namespace Tortuga.Data.Snowflake.Core;

static class SFEnvironment
{
	static SFEnvironment()
	{
		ClientEnv = new LoginRequestClientEnv()
		{
			Application = System.Diagnostics.Process.GetCurrentProcess().ProcessName,
			OSVersion = Environment.OSVersion.VersionString,
#if NETFRAMEWORK
			NetRuntime = "NETFramework",
			NetVersion = "4.7.2",
#else
                NetRuntime = "NETCore",
                NetVersion ="2.0",
#endif
		};

		DriverVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString();
		DriverName = ".NET";
	}

	internal static string DriverName { get; private set; }
	internal static string DriverVersion { get; private set; }
	internal static LoginRequestClientEnv ClientEnv { get; private set; }
}
