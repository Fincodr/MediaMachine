using System;

namespace MediaMachine.Plugins
{
	/// <summary>
	/// A public interface to be used by all custom plugins
	/// </summary>
	public interface IPlugin
	{
        string Name { set;get; }
        string Description { set;get; }
        int MaxWorktime { set;get; }
        void Execute(IPluginContext context);
	}
}
