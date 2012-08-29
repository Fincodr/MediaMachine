using System;
using System.Collections.Generic;
using System.Text;
using MediaMachine.Plugins;

namespace MediaMachineApp
{
    public class PluginContext : IPluginContext
    {
        private string m_sCommands;
        private string m_sIncludedXml;
        private string m_sResults;

        public PluginContext(string sCommands, string sIncludedXml, string sResults)
        {
            m_sCommands = sCommands;
            m_sIncludedXml = sIncludedXml;
            m_sResults = sResults;
        }
        
        #region IPluginContext Members

        public string sIncludedXml
        {
            get
            {
                return m_sIncludedXml;
            }
            set
            {
                m_sIncludedXml = value;
            }
        }

        public string sCommands
        {
            get
            {
                return m_sCommands;
            }
            set
            {
                m_sCommands = value;
            }
        }

        public string sResults
        {
            get
            {
                return m_sResults;
            }
            set
            {
                m_sResults = value;
            }
        }

        #endregion
    }
}
