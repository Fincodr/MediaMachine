using System;

namespace MediaMachine.Plugins
{
	/// <summary>
	/// A public interface used to pass context to plugins
	/// </summary>
	public interface IPluginContext
	{
        string sCommands { get;set; }
        string sIncludedXml { get;set; }
        string sResults { get;set; }
	}
}
