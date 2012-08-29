using System;
using System.Text;
using System.Xml;
using MediaMachine.Plugins;

namespace MediaMachine.Plugins.Debug
{
	/// <summary>
	/// This class implements the IPlugin interface
	/// to provide the ability to parse and isolate email addresses found within
	/// the current editor's text
	/// </summary>
    /// 

    public class DebugPlugin : IPlugin
	{
        private string PluginName = "Debug";
        private string PluginDescription = "Debug Plugin";
        private int PluginMaxWorktime = 60;

        public DebugPlugin()
		{
		}
		#region IPlugin Members

		/// <summary>
		/// The single point of entry to our plugin
		/// Acepts an IPluginContext object which holds the current
		/// context of the running editor.
		/// It then parses the text found inside the editor
		/// and changes it to reflect any email addresses that are found.
		/// </summary>
		/// <param name="context"></param>
		public void Execute(IPluginContext context)
		{
            bool bError = false;            
            string sErrorMsg = "done";
            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(context.sCommands);
            }
            catch
            {
                bError = true;
                sErrorMsg = "Fatal Error: No valid xml structure as commands.";
            }
            if (!bError)
            {
                // Get base element
                switch (xmldoc.FirstChild.Name) {
                    case "output":
                        // We have the default debug structure with output tag, get the text attribute and output to console.
                        string sText = "";
                        try
                        {
                            sText = xmldoc.FirstChild.Attributes["text"].Value.ToString();
                        }
                        catch 
                        {
                        }
                        finally {
                            Console.WriteLine("{0}", sText);
                        }
                        break;
                    default:
                        // We have something different.. not supported yet.
                        bError = true;
                        sErrorMsg = "Fatal Error: Currently only 'output' tag is supported for debug plug-in";
                        break;
                }
            }
            context.sResults = sErrorMsg;
		}

		/// <summary>
		/// The name of the plugins as it will appear 
		/// under the editor's "Plugins" menu
		/// </summary>
		public string Name
		{
			get
			{
                return PluginName;
			}

            set
            {
                PluginName = value;
            }

		}

        /// <summary>
        /// The name of the plugins as it will appear 
        /// under the editor's "Plugins" menu
        /// </summary>
        public int MaxWorktime
        {
            get
            {
                return PluginMaxWorktime;
            }

            set
            {
                PluginMaxWorktime = value;
            }

        }

        /// <summary>
        /// The name of the plugins as it will appear 
        /// under the editor's "Plugins" menu
        /// </summary>
        public string Description
        {
            get
            {
                return PluginDescription;
            }

            set
            {
                PluginDescription = value;
            }

        }

        #endregion

	}
}
