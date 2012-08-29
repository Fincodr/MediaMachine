using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using MediaMachine.Plugins;

namespace MediaMachine.Plugins
{
    /// <summary>
    /// This class implements the IPlugin interface
    /// to provide the ability to parse and isolate email addresses found within
    /// the current editor's text
    /// </summary>
    /// 

    public class AGSPlugin : IPlugin
    {
        private string PluginName = "AGS";
        private string PluginDescription = "Adobe Graphics Server";
        int PluginMaxWorktime = 60;

        AlterCastCOMLib.ACServerClass AC;
        AlterCastCOMLib.ACRequestClass ACr;

        public AGSPlugin()
        {
            
            //Console.WriteLine("AGSPlugIn: Creating AlterCastCOMLib.ACServerClass();");
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
                try
                {
                    //AlterCastCOMLib.ACRequestClass ACr = new AlterCastCOMLib.ACRequestClass();
                    /*
                    string[] texts = context.sCommands.Split('\n');
                    int i=0;
                    foreach (string text in texts)
                    {
                        i++;
                        Console.WriteLine("{0} {1}", i, text);
                    }
                    */
                    AC = new AlterCastCOMLib.ACServerClass();
                    ACr = new AlterCastCOMLib.ACRequestClass();
                    AlterCastCOMLib.ACResponse ACres;
                    ACr.setCommands(context.sCommands);
                    ACres = AC.execute(ACr);
                    ACr = null;
                    AC = null;
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    sErrorMsg = "Fatal Error: " + ex.ToString();
                }
            }
            context.sResults = sErrorMsg;
            //AC = null;
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

        #endregion

    }
}
