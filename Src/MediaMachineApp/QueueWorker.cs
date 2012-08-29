using System;
using System.Threading;
using System.Collections;
using System.IO;
using System.Xml;
using MediaMachine.Plugins;

namespace MediaMachineApp
{
    public class QueueWorker
    {
		private App cApp;

		public QueueWorker(ref App x)
		{
			cApp = x;
		}

        public int QueueFind(string sFileName)
        {
            // find file from queue and return status
            lock (cApp.queue.aQueueFileName)
            {
                for (int i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                {
                    if (cApp.queue.aQueueFilePath[i].Equals(sFileName))
                    {
                        return (i);
                    }
                }
                return (-1);
            }
        }

        public int QueueFindXML(string sFileName)
        {
            string sXmlFile = MediaMachineApp.App.MXGetFileNameWithoutExtension(sFileName) + ".xml";
            return (QueueFind(sXmlFile));
        }


        public string MXRunScript(string sScriptName, string sFileName, string sText)
        {
            Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - Executing script: {0}", sScriptName );
            // send startup mail
            //string sCmds = @"<email from='do.not.reply@wsoy.fi' to='mika.luoma-aho@wsoy.fi' subject='Media Machine stopped at %datetime%' text='' />";
            //string sResults = QueueWorkerClass.PluginExecute("EMAIL", "", sCmds);
            //Console.WriteLine("Results: {0}", sResults);
            string sResults = "";
            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(cApp.settings.sSettingsXml);
                try
                {
                    XmlNodeList nodes1, nodes2;
                    nodes1 = xmldoc.SelectNodes("settings/scripts/script");
                    if (nodes1 != null)
                    {
                        foreach (XmlNode node1 in nodes1)
                        {
                            string sName = "";
                            try
                            {
                                sName = node1.Attributes["name"].Value.ToString();
                            }
                            catch
                            {
                            }
                            if (sName == sScriptName)
                            {
                                //Console.Error.WriteLine("Notice: Executing script: {0}", sName);
                                nodes2 = node1.SelectNodes("commands/command");
                                foreach (XmlNode node2 in nodes2)
                                {                   
                                    string sPluginName = "";
                                    try
                                    {
                                        sPluginName = node2.Attributes["plugin"].Value.ToString();
                                    }
                                    catch
                                    {
                                    }
                                    if (sPluginName != "")
                                    {
                                        sResults = PluginExecuteForScript(sPluginName, sFileName, sText, node2.InnerXml.ToString());
                                    }
                                }
                                break;
                            }

                         
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fatal Error: Exception while running script: {0}", ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal Error: Exception while parsing the settings.xml: {0}", ex.ToString());
            }
            return (sResults);
        }

        public string PluginExecuteForScript(string sPluginName, string sFileName, string sText, string sCommands)
        {
            Hashtable localParams = new Hashtable();
            Hashtable localReplace = new Hashtable();
            Hashtable localFlags = new Hashtable();
            localParams.Add("plugin", sPluginName);
            localReplace.Add("plugin", "");
            localFlags.Add("plugin", "");
            localParams.Add("source", sFileName);
            localReplace.Add("source", "");
            localFlags.Add("source", "");
            localParams.Add("source.xml", "");
            localReplace.Add("source.xml", "");
            localFlags.Add("source.xml", "");
            localParams.Add("source.file", App.MXGetFileNameWithoutExtension(App.MXGetOnlyFileName(sFileName)));
            localReplace.Add("source.file", "");
            localFlags.Add("source.file", "");
            localParams.Add("source.filename", App.MXGetOnlyFileName(sFileName));
            localReplace.Add("source.filename", "");
            localFlags.Add("source.filename", "");
            localParams.Add("source.extension", App.MXGetFileExtension(sFileName));
            localReplace.Add("source.extension", "");
            localFlags.Add("source.extension", "");
            localParams.Add("time", DateTime.Now.ToString("HH:mm"));
            localReplace.Add("time", "");
            localFlags.Add("time", "");
            localParams.Add("date", DateTime.Now.ToString("dd.MM.yyyy"));
            localReplace.Add("date", "");
            localFlags.Add("date", "");
            localParams.Add("timestamp", DateTime.Now.ToString("yyMMddHHmm"));
            localReplace.Add("timestamp", "");
            localFlags.Add("timestamp", "");
            localParams.Add("datetime", DateTime.Now.ToString());
            localReplace.Add("datetime", "");
            localFlags.Add("datetime", "");
            // try to convert the text to the supported xml format
            localParams.Add("text", sText);
            localReplace.Add("text", "");
            localFlags.Add("text", "");
            // add host and ip
            localParams.Add("host", cApp.HOST);
            localReplace.Add("host", "");
            localFlags.Add("host", "");
            localParams.Add("ip", cApp.IP);
            localReplace.Add("ip", "");
            localFlags.Add("ip", "");
            //Console.WriteLine("DEBUG::Executing plugin {0} for script", sPluginName);
            sCommands = App.MXConvertParamsInString(localParams, localReplace, localFlags, sCommands);
            string sResults = PluginExecute(sPluginName, sFileName, sCommands, "");
            //Console.WriteLine("DEBUG::Results = {0} from plugin {1}", sResults,sPluginName);
            return (sResults);
        }

        public string PluginExecute(string sPluginName, string sFileName, string sCommands, string sIncludedXml)
        {
            bool bErrors = false;
            string sResults = "";
            IPlugin hPlugin;
            foreach (IPlugin plugin in App.m_plugins)
            {
                hPlugin = plugin;
                //We add a small ampersand at the start of the name
                //so we can get shortcut key strokes for it
                if (plugin.Name.ToLower() == sPluginName.ToLower())
                {
                    //Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                    //Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " W Executing plug-in: {0} for file: {1}", plugin.Name, App.MXGetOnlyFileName(sFileName));
                    // start worker thread
                    ManualResetEvent manualEvent = new ManualResetEvent(false);
                    PluginContext myContext = new PluginContext(sCommands, sIncludedXml, sResults);
                    if (myContext.sCommands.StartsWith("<![CDATA["))
                    {
                        myContext.sCommands = myContext.sCommands.Substring("<![CDATA[".Length);
                        myContext.sCommands = myContext.sCommands.Substring(0, myContext.sCommands.Length - "]]>".Length);
                    }
                    myContext.sCommands = myContext.sCommands.Trim();
                    Worker WorkerClass = new Worker(ref cApp, ref hPlugin, ref myContext, ref manualEvent);
                    Thread thWorker = new Thread(new ThreadStart(WorkerClass.Main));
                    thWorker.Start();

                    manualEvent.WaitOne(hPlugin.MaxWorktime * 1000, false);
                    if (myContext.sResults != "")
                    {
                        sResults = myContext.sResults;
                    }
                    else
                    {
                        thWorker.Abort();
                        bErrors = true;
                        //Console.Error.WriteLine("Results = {0}", myContext.sResults);
                        sResults = "Fatal Error: Plug-In was running longer than allowed (" + string.Format("{0}", hPlugin.MaxWorktime) + "s)";
                    }
                    break;

                    /*
                    DateTime dFrom, dTo;
                    TimeSpan tSpan;
                    dFrom = DateTime.Now;

                    bool bAbort = false;
                    for (; ; )
                    {
                        dTo = DateTime.Now;
                        tSpan = -dFrom.Subtract(dTo);
                        if (tSpan.TotalSeconds > hPlugin.MaxWorktime)
                        {
                            bAbort = true;
                            break;
                        }
                        Thread.Sleep(250);
                        if (thWorker.ThreadState == ThreadState.Stopped)
                        {
                            bAbort = false;
                            break;
                        }
                    }
                    break;
                    */
                }
            }
            return (sResults);
        }

        public void QueueProcess()
        {
            bool bFound = false;
            int i = 0, jIdx = -1, iStatus = 0;
            int iFile, iXml = -1;
            int iMaxRunTime = -1;
            string sFileName = "";
            string sXmlFileName = "";
            bool bRequireXml = false;
            bool bErrors = false;
            lock (cApp.queue.aQueueFileName)
            {
                if (cApp.queue.aQueueFileName.Count > 0)
                {
                    // go thru queue and check which file are ready for processing
                    for (i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                    {
                        bErrors = false;
                        bRequireXml = false;
                        jIdx = (int)cApp.queue.aQueueJobIdx[i];
                        if (cApp.settings.aFoldersRequireXml[jIdx].ToString() == "true") bRequireXml = true;
                        sFileName = cApp.queue.aQueueFilePath[i].ToString();
                        if (!sFileName.EndsWith(".xml")) {
                            // File is not .xml, file ready for processing?
                            if ((int)cApp.queue.aQueueStatus[i] == 2)
                            {
                                // Yes, do we need xml?
                                iXml = -1;
                                if (bRequireXml) {
                                    // Yes, xml file ready too?
                                    iXml = QueueFindXML(sFileName);
                                    if (iXml != -1)
                                    {
                                        if ((int)cApp.queue.aQueueStatus[iXml] == 2)
                                        {
                                            // Yes, read xml-file contents into memory
                                            sXmlFileName = cApp.queue.aQueueFilePath[iXml].ToString();
                                            //Console.Error.WriteLine("I would now read xml-companion file: {0} into memory to be used by the plug-in", sXmlFileName);
                                        }
                                        else
                                        {
                                            // No, this should not happend. Send file(s) to error.path
                                            Console.Error.WriteLine("FATAL ERROR: File {0} should have xml-companion ready but don't. Sending file to error.path", sFileName);
                                            cApp.queue.aQueueStatus[i] = -1;
                                            cApp.queue.aQueueStatus[iXml] = -1;
                                            bErrors = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        // No, this should not happend. Send file(s) to error.path
                                        Console.Error.WriteLine("FATAL ERROR: File {0} should have xml-companion available but don't. Sending file to error.path", sFileName);
                                        cApp.queue.aQueueStatus[i] = -1;
                                        cApp.queue.aQueueStatus[iXml] = -1;
                                        bErrors = true;
                                        break;
                                    }
                                }
                                if (!bErrors)
                                {
                                    bFound = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (bFound)
            {
                if (!bErrors)
                {
                    // start processing of file
                    string sIncludedXml = "";
                    if (bRequireXml)
                    {
                        string sText;
                        FileStream fsin = new FileStream(sXmlFileName, FileMode.Open);
                        TextReader tin = new StreamReader(fsin, true);
                        try
                        {
                            sIncludedXml = tin.ReadToEnd();
                        }
                        catch
                        {
                        }
                        tin.Close();
                    }
                    // execute needed plug-in(s)
                    lock (cApp.OutputLock)
                    {
                        Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                        Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " W >{0}", App.MXGetOnlyFileName(sFileName));

                    }
                    // read commands
                    XmlDocument XmlDoc = new XmlDocument();
                    string sCommands = cApp.settings.aFoldersXml[jIdx].ToString();
                    XmlDoc.LoadXml(sCommands);
                    XmlNodeList nodes;
			        nodes = XmlDoc.SelectNodes("job/commands/command");
                    if (nodes != null)
                    {
                        string sResults = "";
                        foreach (XmlNode node in nodes)
                        {
                            string sMatch = "";
                            try
                            {
                                sMatch = node.Attributes["match"].Value.ToString();
                            }
                            catch
                            {
                            }
                            bool bProcess = true;
                            if (sMatch != "")
                            {
                                if (sMatch.StartsWith("!="))
                                {
                                    // We need to do a no-match against the input filename
                                    if (sFileName.ToLower().Contains(sMatch.Substring(2).ToLower()))
                                    {
                                        bProcess = false;
                                    }
                                }
                                else
                                {
                                    // We need to do a match against the input filename
                                    if (!sFileName.ToLower().Contains(sMatch.ToLower()))
                                    {
                                        bProcess = false;
                                    }
                                }
                            }
                            if (bProcess)
                            {
                                string sPluginName = "";
                                try
                                {
                                    sPluginName = node.Attributes["plugin"].Value.ToString();
                                }
                                catch
                                {
                                }
                                if (sPluginName != "")
                                {
                                    //Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                                    //Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " P {0}", sPluginName);
                                    Hashtable localParams = new Hashtable(((Hashtable)cApp.settings.aFoldersParameters[jIdx]));
                                    Hashtable localReplace = new Hashtable(((Hashtable)cApp.settings.aFoldersParametersReplace[jIdx]));
                                    Hashtable localFlags = new Hashtable(((Hashtable)cApp.settings.aFoldersParametersFlags[jIdx]));
                                    localParams.Add("plugin", sPluginName);
                                    localReplace.Add("plugin", "");
                                    localFlags.Add("plugin", "");
                                    localParams.Add("source", sFileName);
                                    localReplace.Add("source", "");
                                    localFlags.Add("source", "");
                                    localParams.Add("source.xml", sXmlFileName);
                                    localReplace.Add("source.xml", "");
                                    localFlags.Add("source.xml", "");
                                    localParams.Add("source.file", App.MXGetFileNameWithoutExtension(App.MXGetOnlyFileName(sFileName)));
                                    localReplace.Add("source.file", "");
                                    localFlags.Add("source.file", "");
                                    localParams.Add("source.filename", App.MXGetOnlyFileName(sFileName));
                                    localReplace.Add("source.filename", "");
                                    localFlags.Add("source.filename", "");
                                    localParams.Add("source.extension", App.MXGetFileExtension(sFileName));
                                    localReplace.Add("source.extension", "");
                                    localFlags.Add("source.extension", "");
                                    localParams.Add("time", DateTime.Now.ToString("HH:mm"));
                                    localReplace.Add("time", "");
                                    localFlags.Add("time", "");
                                    localParams.Add("date", DateTime.Now.ToString("dd.MM.yyyy"));
                                    localReplace.Add("date", "");
                                    localFlags.Add("date", "");
                                    localParams.Add("timestamp", DateTime.Now.ToString("yyMMddHHmm"));
                                    localReplace.Add("timestamp", "");
                                    localFlags.Add("timestamp", "");
                                    localParams.Add("datetime", DateTime.Now.ToString());
                                    localReplace.Add("datetime", "");
                                    localFlags.Add("datetime", "");
                                    // add host and ip
                                    localParams.Add("host", cApp.HOST);
                                    localReplace.Add("host", "");
                                    localFlags.Add("host", "");
                                    localParams.Add("ip", cApp.IP);
                                    localReplace.Add("ip", "");
                                    localFlags.Add("ip", "");
                                    //Console.WriteLine("{0} {1}", localParams.Count, localReplace.Count);
                                    //string sCommand = App.MXConvertParamsInString((Hashtable)cApp.settings.aFoldersParameters[jIdx], (Hashtable)cApp.settings.aFoldersParametersReplace[jIdx], node.InnerXml.ToString());
                                    // replace commands with job and local parameters
                                    string sCommand = App.MXConvertParamsInString(localParams, localReplace, localFlags, node.InnerXml.ToString());
                                    // replace %{xml://xpath,attribute}% with value from companion xml
                                    sCommand = App.MXConvertXPathsInString(sIncludedXml, sCommand);
                                    sResults = PluginExecute(sPluginName, sFileName, sCommand, sIncludedXml);
                                    if (sResults != "done")
                                    {
                                        bErrors = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (bErrors)
                        {
                            // fatal error
                            // set file(s) done
                            Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " ! " + sResults);
                            lock (cApp.queue)
                            {
                                i = QueueFind(sFileName);
                                if (i != -1) cApp.queue.aQueueStatus[i] = -1;
                                if (bRequireXml)
                                {
                                    iXml = QueueFindXML(sFileName);
                                    if (iXml != -1) cApp.queue.aQueueStatus[iXml] = -1;
                                }
                            }
                        }
                        else
                        {
                            // ok?
                            // set file(s) done
                            Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " q <{0}", App.MXGetOnlyFileName(sFileName));
                            lock (cApp.queue)
                            {
                                i = QueueFind(sFileName);
                                if (i != -1) cApp.queue.aQueueStatus[i] = 3;
                                if (bRequireXml)
                                {
                                    iXml = QueueFindXML(sFileName);
                                    if (iXml != -1) cApp.queue.aQueueStatus[iXml] = 3;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Main()
        {
            for (; ; )
            {
                Thread.Sleep(250);
                if (!cApp.bRunning) break;
                QueueProcess();
            }
        }

        
    }
}
