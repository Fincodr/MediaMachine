using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MediaMachine.Plugins;

namespace MediaMachineApp
{
    // worker class
    //

    class Worker
    {
    	public static App cApp;
        public static IPlugin cPlugin;
        public static PluginContext cPluginContext;
        public static ManualResetEvent cResetEvent;

		public Worker(ref App x, ref IPlugin plg, ref PluginContext ctx, ref ManualResetEvent rev)
		{
			cApp = x;
            cPlugin = plg;
            cPluginContext = ctx;
            cResetEvent = rev;
		}

        public void Main()
        {
            //Console.WriteLine("Worker::context.sCommands = {0}", cPluginContext.sCommands);
            cPlugin.Execute(cPluginContext);
            //Console.WriteLine("Worker::context.sResults = {0}", cPluginContext.sResults);
            cResetEvent.Set();
        }

    }
}
