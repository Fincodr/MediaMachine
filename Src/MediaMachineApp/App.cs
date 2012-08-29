using System;
using System.Collections;
using System.Xml;
using System.IO;
using System.Threading;
using MediaMachine.Plugins;

namespace MediaMachineApp
{
	/// <summary>
	/// Summary description for App.
	/// </summary>
	public class App
	{
		public bool bRunning = true;
        static string VERSION = "1.1-20100617";
        public string HOST = "";
        public string IP = "";
        static public PluginCollection m_plugins = new PluginCollection();
        static App cApp;
        public ArrayList MemoryLog;
        static FileStream Pid_File;
        static StreamWriter Pid_Stream;

        public class QueueClass
        {
            public ArrayList aQueueFileName;
            public ArrayList aQueueFilePath;
            public ArrayList aQueueTimeStamp;
            public ArrayList aQueueStatus;
            public ArrayList aQueueJobIdx;
            public ArrayList aQueueElapsed;

            public bool Contains(string sFile)
            {
                if (aQueueFilePath.Contains(sFile)) return (true); else return (false);
            }

            public bool Remove(string sFile)
            {
                lock (aQueueFilePath)
                {
                    if (aQueueFilePath.Contains(sFile))
                    {
                        // file in queue, remove anyway ---(if not processing) and return true.
                        int iIdx = aQueueFilePath.IndexOf(sFile);
                        /*if ((int)aQueueStatus[iIdx] == 2)
                        {
                            // file is in processing, do not remove. return false.
                            return (false);
                        }
                        else
                        {*/
                            aQueueFileName.RemoveAt(iIdx);
                            aQueueFilePath.RemoveAt(iIdx);
                            aQueueTimeStamp.RemoveAt(iIdx);
                            aQueueStatus.RemoveAt(iIdx);
                            aQueueJobIdx.RemoveAt(iIdx);
                            aQueueElapsed.RemoveAt(iIdx);
                            return (true);
                        //}
                    }
                    else
                    {
                        // file not in queue, return false.
                        return (false);
                    }
                }
            }

            public bool Add(string sFile, int sJobIdx)
            {
                lock (aQueueFilePath)
                {
                    if (aQueueFilePath.Contains(sFile))
                    {
                        // file already in queue, update timestamp and return false.
                        int iIdx = aQueueFilePath.IndexOf(sFile);
                        aQueueTimeStamp[iIdx] = DateTime.Now;
                        return (false);
                    }
                    else
                    {
                        // add to queue and return true.
                        aQueueFileName.Add(App.MXGetOnlyFileName(sFile));
                        aQueueFilePath.Add(sFile);
                        aQueueStatus.Add((int)0);    // status: 0=waiting, 1=ready for processing, 2=processing, 3=finished/error, -1=ignored
                        aQueueTimeStamp.Add(DateTime.Now);
                        aQueueJobIdx.Add(sJobIdx);
                        aQueueElapsed.Add((double)0);
                        return (true);
                    }
                }
            }

            public bool Rename(string sOldFile, string sFile, int sJobIdx)
            {
                lock (aQueueFilePath)
                {
                    if (aQueueFilePath.Contains(sOldFile))
                    {
                        // file already in queue, update new name, timestamp and return false.
                        int iIdx = aQueueFilePath.IndexOf(sOldFile);
                        aQueueFileName[iIdx] = App.MXGetOnlyFileName(sFile);
                        aQueueFilePath[iIdx] = sFile;
                        aQueueTimeStamp[iIdx] = DateTime.Now;
                        aQueueElapsed[iIdx] = 0;
                        return (false);
                    }
                    else
                    {
                        // this is rename so this should not happened but whatever. Lets add this file as new
                        aQueueFileName.Add(App.MXGetOnlyFileName(sFile));
                        aQueueFilePath.Add(sFile);
                        aQueueStatus.Add((int)0);    // status: 0=waiting, 1=ready for processing, 2=processing, 3=finished/error, -1=ignored
                        aQueueTimeStamp.Add(DateTime.Now);
                        aQueueJobIdx.Add(sJobIdx);
                        aQueueElapsed.Add((double)0);
                        return (true);
                    }
                }
            }
        
        }

		public class SettingsClass
		{
			public string sIniFile;
			public string sLogFile;
            public string sSettingsXml;
            public int iScanDelay;
			public int iTimeOut;
			public int iWaitTime;
            public int iIgnoreTime;
            public int iMaxThreads;
			public ArrayList aFoldersPath;
			public ArrayList aFoldersSubfolders;
			public ArrayList aFoldersName;
			public ArrayList aFoldersXml;
            public ArrayList aFoldersFilter;
            public ArrayList aFoldersRequireXml;
            public ArrayList aFoldersRequireExt;
            public ArrayList aFoldersWaitTime;
            public ArrayList aFoldersIgnoreTime;
            public ArrayList aFoldersParameters;
            public ArrayList aFoldersParametersReplace;
            public ArrayList aFoldersParametersFlags;
        }

		public SettingsClass settings;
        public QueueClass queue;
		public object OutputLock = new Object();

		public App()
		{

			settings = new SettingsClass();
			settings.aFoldersPath = new ArrayList();
			settings.aFoldersSubfolders = new ArrayList();
			settings.aFoldersName = new ArrayList();
			settings.aFoldersXml = new ArrayList();
            settings.aFoldersFilter = new ArrayList();
            settings.aFoldersRequireXml = new ArrayList();
            settings.aFoldersRequireExt = new ArrayList();
            settings.aFoldersWaitTime = new ArrayList();
            settings.aFoldersIgnoreTime = new ArrayList();
            settings.aFoldersParameters = new ArrayList();
            settings.aFoldersParametersReplace = new ArrayList();
            settings.aFoldersParametersFlags = new ArrayList();

            queue = new QueueClass();
            queue.aQueueFileName = new ArrayList();
            queue.aQueueFilePath = new ArrayList();
            queue.aQueueTimeStamp = new ArrayList();
            queue.aQueueStatus = new ArrayList();
            queue.aQueueJobIdx = new ArrayList();
            queue.aQueueElapsed = new ArrayList();

            cApp = this;

        }

        static public void LoadPlugins()
        {
            //Retrieve a plugin collection using our custom Configuration section handler
            //m_plugins = (PluginCollection)ConfigurationSettings.GetConfig("plugins");
            //Console.Error.WriteLine("");
            foreach (IPlugin plugin in m_plugins)
            {
                //We add a small ampersand at the start of the name
                //so we can get shortcut key strokes for it
                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - Plug-in '{1}' ({0}), loaded.", plugin.Name, plugin.Description);
            }
        }

        static public void ShowBanner()
        {
            ///                      12345678901234567890123456789012345678901234567890123456789012345678901234567890
            ///                      

            Console.WriteLine("");
            Console.WriteLine("===============================================================================");
            Console.WriteLine("{0} EVENT - MediaMachine v" + VERSION + " STARTED", DateTime.Now.ToString("yyMMdd-HH:mm"));
            Console.WriteLine("===============================================================================");

        }

        static public void EndBanner()
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("{0} EVENT - MediaMachine v" + VERSION + " ENDED", DateTime.Now.ToString("yyMMdd-HH:mm"));
            Console.WriteLine("===============================================================================");
            Console.WriteLine("");
        }

        static public string XmlGetProp(XmlDocument XmlDoc, string sProp)
        {
            string sValue;
            try
            {
                sValue = XmlDoc.SelectSingleNode("settings/application/property[@name='" + sProp + "']").Attributes.GetNamedItem("value").Value;
            }
            catch (Exception)
            {

                return (null);
            }
            return (sValue);
        }

        static public bool MXLockPid()
        {
            //Console.WriteLine("MXLockPid()");
            try {
                Pid_File = new FileStream("MediaMachine.pid", FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                Pid_Stream = new StreamWriter(Pid_File, System.Text.Encoding.Default);
                return (true);
            }
            catch (Exception ex) {
                //Console.WriteLine("Exception: " + ex.ToString());
                // can't open exclusive access for pid file so the program must be running..
                return (false);
            }
        }

        static public bool MXUnLockPid()
        {
            if (Pid_File != null)
            {
                if (Pid_Stream != null)
                {
                    try
                    {
                        Pid_Stream.Close();
                        File.Delete("MediaMachine.pid");
                    }
                    catch
                    {
                    }
                    
                    return (true);
                }
            }
            return (false);
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

        static public string MXGetFileNameWithoutExtension(string sFileName)
        {
            int s1 = sFileName.LastIndexOf(".");
            if (s1 != -1)
            {
                return (sFileName.Substring(0, s1));
            }
            else
            {
                return (sFileName);
            }
        }

        static public string MXGetFileExtension(string sFileName)
        {
            int s1 = sFileName.LastIndexOf(".");
            if (s1 != -1)
            {
                return (sFileName.Substring(s1 + 1));
            }
            else
            {
                return (sFileName);
            }
        }

        static public bool MXFileLocked(string sSource)
        {
            //Console.WriteLine("Trying to read file = {0}", sSource);
            try
            {
                FileStream fs = new FileStream(sSource, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                fs.Close();
            }
            catch (Exception ex)
            {
                //Console.WriteLine("MXFileLocked:{0}={1}", sSource, ex.ToString());
                return (true);
            }
            return(false);
        }

        static public void MXMoveFile(string sSource, string sTarget, bool bOverwrite)
        {
            if (!File.Exists(sSource)) return;
            ////////////////
            //
            // MXMoveFile
            //
            ////////////////
            bool bFileLocked = false;
            string sNewTarget = MXGetFileNameWithoutExtension(sTarget) + "." + DateTime.Now.ToString("yyMMdd-HHmm") + "." + MXGetFileExtension(sTarget);
            string sNewSource = MXGetFileNameWithoutExtension(sSource) + "." + DateTime.Now.ToString("yyMMdd-HHmm") + "." + MXGetFileExtension(sSource) + ".locked.or.other.error";

            // 1.) Check if the source file is read-only -> remove attributes
            if ((File.GetAttributes(sSource) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                // clear file attributes
                try
                {
                    File.SetAttributes(sSource, FileAttributes.Normal);
                }
                catch
                {
                    // File could be locked or no access to attiributes. continued.
                }
            }

            // 2.) Try to move the file
            try
            {
                File.Move(sSource, sTarget);
            }
            catch
            {
                // Error while moving file
                // If the target file exists, try to remove it and move again
                if (File.Exists(sTarget))
                {
                    // Yes, target file exists
                    // 1.) Do we have right to delete target?
                    if (bOverwrite)
                    {
                        // Yes, check if target file has read-only attribute
                        if ((File.GetAttributes(sTarget) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            // Yes, clear file attributes
                            try
                            {
                                File.SetAttributes(sTarget, FileAttributes.Normal);
                            }
                            catch
                            {
                                // Can't change attributes. continued.
                            }
                        }
                        // Try to delete file and then move
                        try
                        {
                            File.Delete(sTarget);
                            File.Move(sSource, sTarget);
                        }
                        catch
                        {
                            // Error: Can't remove target file or other error while moving
                            // Try to rename while moving
                            try
                            {
                                File.Move(sSource, sNewTarget);
                            }
                            catch
                            {
                                // Error: Can't even move while renaming so we must have file lock
                            }
                        }
                    }
                    else
                    {
                        // No right to delete target so rename and move
                        try
                        {
                            File.Move(sSource, sNewTarget);
                        }
                        catch
                        {
                            // Error: Can't even move while renaming so we must have file lock
                            // lets try final time to only rename the source
                            try
                            {
                                File.Move(sSource, sNewSource);
                            }
                            catch
                            {
                                // Error: Can't even rename so we must have file lock or no access
                            }
                        }
                    }
                }
                else
                {
                    // target file doesn't exist so we could have
                    // some other error. still let's try one more time
                    // with renaming
                    try
                    {
                        File.Move(sSource, sNewTarget);
                    }
                    catch
                    {
                        // Error: Can't even move while renaming so we must have file lock
                        // lets try final time to only rename the source
                        try
                        {
                            File.Move(sSource, sNewSource);
                        }
                        catch
                        {
                            // Error: Can't even rename so we must have file lock or no access
                        }
                    }
                }
            }
        }

        // Process all files in the directory passed in, and recurse on any directories 
        // that are found to process the files they contain
        public static void MXProcessDirectory(string targetDirectory, bool includeSubs)
        {
            try
            {
                // Process the list of files found in the directory
                string[] fileEntries = Directory.GetFiles(targetDirectory);
                foreach (string fileName in fileEntries)
                    MXProcessFile(fileName);

                if (includeSubs)
                {
                    // Recurse into subdirectories of this directory
                    string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
                    foreach (string subdirectory in subdirectoryEntries)
                        MXProcessDirectory(subdirectory, includeSubs);
                }
            }
            catch
            {
            }
        }

        // Real logic for processing found files would go here.
        public static void MXProcessFile(string path)
        {
            // find job by fullpath
            int jIdx = -1;
            for (int i = 0; i < cApp.settings.aFoldersPath.Count; i++)
            {
                if (path.StartsWith(cApp.settings.aFoldersPath[i].ToString()))
                {
                    jIdx = i;
                    break;
                }
            }
            cApp.queue.Add(path, jIdx);
        }

        public static string MXGetFlags(Hashtable flagsTable, string sParam)
        {
            string sFlags = "";
            try
            {
                sFlags = (string)flagsTable[sParam];
            }
            catch
            {
                return ("");
            }
            return (sFlags);

        }


        // Get parameter from parameters array with real-time replace of parameter values ;)
        public static string MXGetParam(Hashtable paramTable, Hashtable replaceTable, string sParam)
        {
            string sValue = "";
            string sReplace = "";
            string sWith = "";
            try
            {
                sValue = (string)paramTable[sParam];
                if (replaceTable != null)
                {
                    try
                    {
                        string[] tmp = replaceTable[sParam].ToString().Split('|');
                        sReplace = tmp[0];
                        sWith = tmp[1];
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                // no parameter found, return empty.
                return ("");
            }
            if (sValue != null)
            {
                foreach (string sKey in paramTable.Keys)
                {
                    sValue = sValue.Replace("%" + sKey + "%", paramTable[sKey].ToString());
                }
            }
            if (sReplace != "")
            {
                sValue = sValue.Replace(sReplace, sWith);
            }
            return (sValue);
        }

        // Convert string with parameters
        public static string MXConvertParamsInString(Hashtable paramTable, Hashtable replaceTable, Hashtable flagsTable, string sParams)
        {
            string sValue;
            string sFlags;
            foreach (string sKey in paramTable.Keys)
            {
                sValue = MXGetParam(paramTable, replaceTable, sKey);
                sFlags = MXGetFlags(flagsTable, sKey);
                if (sFlags.ToLower().Contains("tolower"))
                {
                    sValue = sValue.ToLower();
                }
                sParams = sParams.Replace("%" + sKey + "%", sValue);
            }
            return (sParams);
        }

        // Convert string with xpath values from companion xml
        public static string MXConvertXPathsInString(string sIncludedXml, string sText)
        {
            // %{xml://xpath,attribute}%
            bool bErrors = false;
            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(sIncludedXml);
            }
            catch 
            {
                bErrors = true;
            }
            if (!bErrors)
            {
                //XmlNode node;
                // go thru text and find every %{xml://}
                //Console.WriteLine("MXConvertXPathsInString: '{0}'", sIncludedXml);
                int s1 = -1;
                int s2;
                while (1==1) {
                    s1 = sText.IndexOf("%{xml://", s1+1);
                    if (s1 == -1) break;
                    s1 += "%{xml://".Length;
                    s2 = sText.IndexOf("}%", s1);
                    if (s2 != -1)
                    {
                        try
                        {
                            string[] texts = sText.Substring(s1, s2 - s1).Split(',');
                            string sXPath = texts[0];
                            string sAttribute = texts[1];
                            string sValue = "";
                            //Console.WriteLine("Found: {0}", sText.Substring(s1, s2 - s1));
                            //Console.WriteLine("XPath = '{0}'", sXPath);
                            //Console.WriteLine("Attribute name = '{0}'", sAttribute);
                            if (sAttribute == "#PCDATA")
                            {
                                sValue = xmldoc.SelectSingleNode(sXPath).InnerText.ToString();
                            }
                            else
                            {
                                sValue = xmldoc.SelectSingleNode(sXPath).Attributes.GetNamedItem(sAttribute).Value;
                            }
                            //Console.WriteLine("{0} = {1}", sXPath, sValue);
                            //Console.WriteLine("< '{0}'", sText.Substring(s1 - 15, 77));
                            sText = sText.Substring(0, s1 - "%{xml://".Length) + sValue + sText.Substring(s2 + 2);
                            //Console.WriteLine("> '{0}'", sText.Substring(s1 - 15, 77));
                        }
                        catch
                        {
                        }
                    }
                }

            }
            //Console.WriteLine(sText);
            return (sText);
        }

    }
}
