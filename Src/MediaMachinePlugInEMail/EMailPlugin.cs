using System;
using System.Text;
using System.Xml;
using System.Net.Mail;
using MediaMachine.Plugins;

namespace MediaMachine.Plugins
{
    /// <summary>
    /// This class implements the IPlugin interface
    /// to provide the ability to parse and isolate email addresses found within
    /// the current editor's text
    /// </summary>
    /// 

    public class EMailPlugin : IPlugin
    {
        private string PluginName = "EMail";
        private string PluginDescription = "EMail Plugin";
        private int PluginMaxWorktime = 60;

        public EMailPlugin()
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
                switch (xmldoc.FirstChild.Name)
                {
                    case "email":
                        // We have the default email structure, get the attributes.
                        string sFrom = "";
                        string sTo = "";
                        string sSubject = "";
                        string sSmtp = "";
                        string sText = "";
                        try
                        {
                            sFrom = xmldoc.FirstChild.Attributes["from"].Value.ToString();
                            sTo = xmldoc.FirstChild.Attributes["to"].Value.ToString();
                            sSubject = xmldoc.FirstChild.Attributes["subject"].Value.ToString();
                            sSmtp = xmldoc.FirstChild.Attributes["smtp"].Value.ToString();
                            sText = xmldoc.FirstChild.InnerXml.ToString();
                            if (sText.StartsWith("<![CDATA["))
                            {
                                sText = sText.Substring("<![CDATA[".Length);
                                sText = sText.Substring(0, sText.Length - "]]>".Length);
                            }
                            try
                            {
                                MailMessage message = new MailMessage(sFrom, sTo, sSubject, sText);
                                SmtpClient emailClient = new SmtpClient(sSmtp);
                                emailClient.Send(message);
                            }
                            catch (Exception ex)
                            {
                                bError = true;
                                sErrorMsg = "Fatal Error: Exception while sending mail: " + ex.ToString();
                            }
                        }
                        catch
                        {
                        }
                        break;
                    default:
                        // We have something different.. not supported yet.
                        bError = true;
                        sErrorMsg = "Fatal Error: Currently only 'email' tag is supported for debug plug-in";
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
