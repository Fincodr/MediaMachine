using System;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using System.IO;
using MediaMachine.Plugins;

namespace MediaMachine.Plugins
{
	/// <summary>
	/// This class implements the IPlugin interface
	/// to provide the ability to parse and isolate email addresses found within
	/// the current editor's text
	/// </summary>
    /// 

    public class DosPlugin : IPlugin
	{
        private string PluginName = "Debug";
        private string PluginDescription = "Debug Plugin";
        private int PluginMaxWorktime = 60;

        public DosPlugin()
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
            XmlNodeList nodes;
            try
            {
                xmldoc.LoadXml(context.sCommands);
            }
            catch
            {
                sErrorMsg = "Fatal Error: No valid xml structure as commands.";
                bError = true;
            }
            if (!bError) {
                // go thru commands/command nodes
                string sSource = "", sTarget = "", sOverwrite = "", sCmd = "", sArgs = "";
                try
                {
                    nodes = xmldoc.SelectNodes("commands/command");
                    foreach (XmlNode node in nodes)
                    {
                        try
                        {
                            switch (node.Attributes["action"].Value.ToString())
                            {
                                case "move":
                                    // move file, source -> target (with overwrite flag)
                                    if (node.Attributes["source"] != null) { sSource = node.Attributes["source"].Value.ToString(); } else { sSource = ""; }
                                    if (node.Attributes["target"] != null) { sTarget = node.Attributes["target"].Value.ToString(); } else { sTarget = ""; }
                                    if (node.Attributes["overwrite"] != null) { sOverwrite = node.Attributes["overwrite"].Value.ToString(); } else { sOverwrite = ""; }
                                    if ((sSource != "") && (sTarget != ""))
                                    {
                                        bool bOverwrite = false;
                                        if (sOverwrite.ToLower().ToString() == "true") bOverwrite = true;
                                        if (!File.Exists(sSource))
                                        {
                                            sErrorMsg = String.Format("Fatal Error: (DosPlugIn) Source file doesn't exists ({0})", sSource);
                                            break;
                                        }
                                        else
                                        {
                                            if (sTarget.EndsWith(@"\"))
                                            {
                                                sTarget = String.Concat(sTarget, MXGetOnlyFileName(sSource));
                                            }
                                            try
                                            {
                                                File.Move(sSource, sTarget);
                                            }
                                            catch
                                            {
                                                // maybe the target file exists?
                                                if ((File.Exists(sTarget)) && (bOverwrite))
                                                {
                                                    File.Delete(sTarget);
                                                    try
                                                    {
                                                        File.Move(sSource, sTarget);
                                                    }
                                                    catch
                                                    {
                                                        // no luck. Abort.
                                                        sErrorMsg = String.Format("Fatal Error: (DosPlugIn) Can't move (overwrite?) target file ({0}->{1})", sSource, sTarget);
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    sErrorMsg = String.Format("Fatal Error: (DosPlugIn) Can't move (overwrite?) target file ({0}->{1})", sSource, sTarget);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case "rename":
                                    // rename file, source -> target
                                    if (node.Attributes["source"] != null) { sSource = node.Attributes["source"].Value.ToString(); } else { sSource = ""; }
                                    if (node.Attributes["target"] != null) { sTarget = node.Attributes["target"].Value.ToString(); } else { sTarget = ""; }
                                    Console.WriteLine("DOS::RENAME {0} -> {1}", sSource, sTarget);
                                    break;
                                case "run":
                                    // run command
                                    if (node.Attributes["cmd"] != null) { sCmd = node.Attributes["cmd"].Value.ToString(); } else { sCmd = ""; }
                                    if (node.Attributes["args"] != null) { sArgs = node.Attributes["args"].Value.ToString(); } else { sArgs = ""; }
                                    if ((sCmd == "") || (sArgs == ""))
                                    {
                                        // fatal error:
                                        sErrorMsg = "Fatal Error: (DosPlugIn) Cmd or Args attribute is missing.";
                                        break;
                                    }
                                    if (!File.Exists(sCmd))
                                    {
                                        sErrorMsg = "Fatal Error: (DosPlugIn) Specified command not found.";
                                        break;
                                    }
                                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                                    proc.EnableRaisingEvents = false;
                                    proc.StartInfo.FileName = sCmd;
                                    proc.StartInfo.Arguments = sArgs;
                                    proc.Start();
                                    proc.WaitForExit();
                                    proc.Close();
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    sErrorMsg = "Fatal Error: No valid xml structure as commands/command.";
                    bError = true;
                }
            }
            context.sResults = sErrorMsg;
		}

        static public string MXGetOnlyFileName(string sFileName)
        {
            int s1 = sFileName.LastIndexOf("\\");
            if (s1 != -1)
            {
                return (sFileName.Substring(s1 + 1));
            }
            else
            {
                return (sFileName);
            }
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
