using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections;
using System.Xml;
using System.Net;
using MediaMachine.Plugins;

namespace MediaMachineApp
{
    class Program
    {
        static App cApp;
        static QueueWorker QueueWorkerClass;

        static private string sLastRefresh;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] args)
		{
            ///////////
            // Initialize
            bool bRestart;
            bRestart = false;
			bool bQuit = false;
			int max_worker = 0;
			int max_port = 0;
			ArrayList WatchedFolders = new ArrayList();
			App AppClass = new App();
            cApp = AppClass;

            cApp.HOST = Dns.GetHostName();
            IPHostEntry iphe;
            iphe = Dns.GetHostByName(cApp.HOST);
            IPAddress[] ipAddresses = iphe.AddressList;
            cApp.IP = "";
            for (int i = 0; i < ipAddresses.GetUpperBound(0); i++)
            {
                if (i > 0) cApp.IP += ",";
                cApp.IP += ipAddresses[i].ToString();
            }
            //Console.WriteLine("{0},{1}", cApp.IP, cApp.HOST);

            // start queue thread
            Queue QueueClass = new Queue(ref AppClass);
            Thread thQueue = new Thread(new ThreadStart(QueueClass.Main));
            thQueue.Start();

            // start queue processing thread
            QueueWorkerClass = new QueueWorker(ref AppClass);
            Thread thQueueWorker = new Thread(new ThreadStart(QueueWorkerClass.Main));
            thQueueWorker.Start();

			AppClass.settings.sIniFile = "settings.xml";
			AppClass.settings.sLogFile = "logfile.txt";

            FileSystemWatcher IniWatcher = new FileSystemWatcher();
            IniWatcher.Path = ".";
            IniWatcher.Filter = AppClass.settings.sIniFile;
            IniWatcher.Changed += new FileSystemEventHandler(OnChanged);
            IniWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;
            IniWatcher.EnableRaisingEvents = true;

            if (!LoadSettings())
            {
                bQuit = true;
                cApp.bRunning = false;
            }

            bool bPidExisted = false;
            if (File.Exists("MediaMachine.pid"))
            {
                bPidExisted = true;
            }
            if (!App.MXLockPid())
            {
                // Can't lock pid file
                string sTempResults = QueueWorkerClass.MXRunScript("alreadyrunning()", "", "Cant lock pid file MediaMachine.pid, so the application must be already running.");
                if (sTempResults != "done")
                {
                    Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " Warning: Problems while executing script 'alreadyrunning()', Error msg = {0}", sTempResults);
                }
                bQuit = true;
                cApp.bRunning = false;
            }

            if (!bQuit)
            {
                ///////////
                // Start console and log writer
                ArrayList MemoryLog = new ArrayList();
                cApp.MemoryLog = MemoryLog;
                SplitWriter OutWriter = new SplitWriter(AppClass.settings.sLogFile, true, ref MemoryLog);
                Console.SetOut(OutWriter);

                //////////////////////////////////////////
                //
                // MAIN LOOP
                //
                //////////////////////////////////////////
                App.ShowBanner();

                // show current settings
                ThreadPool.GetMaxThreads(out max_worker, out max_port);
                AppClass.settings.iMaxThreads = max_worker;
                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - Default settings:");
                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO -   TimeOut: {0} seconds", AppClass.settings.iTimeOut);
                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO -   WaitTime: {0} seconds", AppClass.settings.iWaitTime);
                if ( AppClass.settings.iScanDelay != 0 )
                    Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO -   ScanDelay: {0} seconds", AppClass.settings.iScanDelay);
                else
                    Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO -   ScanDelay: Using system events");
                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO -   IgnoreTime: {0} seconds", AppClass.settings.iIgnoreTime);
                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - OS System status:");
                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO -   Maximum Worker threads: {0}", max_worker);
                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO -   Maximum Aynchronous I/O threads: {0}", max_port);

                // load plugins
                App.LoadPlugins();

                //AutoResetEvent mainEvent = new AutoResetEvent(false);

                // start monitor thread
                Monitor MonitorClass = new Monitor(ref AppClass, ref QueueWorkerClass);
                Thread thMonitor = new Thread(new ThreadStart(MonitorClass.Main));
                thMonitor.Start();

                // add files from job folders
                foreach (string sPath in AppClass.settings.aFoldersPath)
                {
                    App.MXProcessDirectory(sPath, false);
                }

                lock (Console.Out)
                {
                    if (bPidExisted)
                    {
                        // read logfiles last 10 lines into memory
                        FileStream fsin = new FileStream("logfile.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        TextReader tin = new StreamReader(fsin, true);
                        // read log
                        ArrayList tmpArray = new ArrayList();
                        string sLog = "... Last 10 lines from logfile ...\n\n";
                        while (tin.Peek() != -1)
                        {
                            string sTmp = tin.ReadLine();
                            tmpArray.Add(sTmp);
                        }
                        tin.Close();
                        // get last 10 lines... first try to find place where we have the startup banner
                        int i = 0, k = 0;
                        for (int j = tmpArray.Count-1; j >= 0; j--)
                        {
                            if (tmpArray[j].ToString().StartsWith("==================="))
                            {
                                if (tmpArray[j + 1].ToString().Contains("EVENT"))
                                {
                                    k = j;
                                    if (k < 0) k = 0;
                                    i = j - 12;
                                    if (i<0) i=0;
                                    break;
                                }
                            }
                        }
                        for (int j = i; j < k; j++)
                        {
                            sLog += tmpArray[j] + "\n";
                        }
                        sLog += "... end of log ...\n";
                        string sTempResults = QueueWorkerClass.MXRunScript("unplannedshutdown()", "", sLog);
                        if (sTempResults != "done")
                        {
                            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " Warning: Problems while executing script 'unplannedshutdown()', Error msg = {0}", sTempResults);
                        }  
                    }
                }

                lock (Console.Out)
                {
                    // launch startup script
                    string sLog = "";
                    foreach (string sTmp in MemoryLog)
                    {
                        sLog += sTmp + "\n";
                    }
                    string sTempResults = QueueWorkerClass.MXRunScript("startup()", "", sLog);
                    if (sTempResults != "done")
                    {
                        Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " Warning: Problems while executing script 'startup()', Error msg = {0}", sTempResults);
                    } 
                    MemoryLog.Clear();
                }
                

                for (; ; )
                {
                    string sCmd = Console.ReadLine();
                    switch (sCmd)
                    {
                        case "h":
                        case "help":
                        case "?":
                            lock (Console.Error)
                            {
                                Console.Error.WriteLine(
                                    "\n" +
                                    "  Commands:\n" +
                                    "   h/help/?    display this help\n" +
                                    "   q/quit      quit this program (use ctrl+c to force quit)\n" +
                                    "   l/list      list wathced folder(s)\n" +
                                    "   s/status    show status\n" +
                                    "   c/clear     clear the console\n" +
                                    "   r/restart   restart the application\n"
                                    );
                            }
                            break;
                        case "quit":
                            bQuit = true;
                            break;
                    }
                    if (sCmd == "r" || sCmd == "restart")
                    {
                        bRestart = true;
                        break;
                    }
                    if (sCmd == "s" || sCmd == "status")
                    {
                        lock (cApp.queue.aQueueFileName)
                        {
                            Console.Error.WriteLine("==================================");
                            for (int i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                            {
                                try
                                {
                                    DateTime dFrom, dTo;
                                    TimeSpan tSpan;
                                    dFrom = DateTime.Now;
                                    dTo = (DateTime)cApp.queue.aQueueTimeStamp[i];
                                    tSpan = dFrom.Subtract(dTo);
                                    //cApp.queue.aQueueElapsed[i] = (double)tSpan.TotalSeconds;

                                    Console.Error.WriteLine("#{0} {1} {2} {3}", i, cApp.queue.aQueueStatus[i], tSpan.TotalSeconds.ToString(), cApp.queue.aQueueFileName[i]);
                                }
                                catch
                                {
                                    Console.Error.WriteLine("#{0} - error processing", i);
                                }
                            }
                            Console.Error.WriteLine("----------------------------------");

                        }
                    }
                    if (sCmd == "c" || sCmd == "clear")
                        Console.Clear();
                    if (sCmd == "q") break;
                    if (bQuit) break;
                    Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                    Console.Error.Write("{0}{1} > ", '\x0d', DateTime.Now.ToString("yyMMdd-HH:mm"));
                }

                AppClass.bRunning = false;

                // wait for Monitor thread to stop
                for (; ; )
                {
                    if (thMonitor.ThreadState == System.Threading.ThreadState.Stopped)
                        break;
                    Thread.Sleep(50);
                }

                // wait for Queue thread to stop
                for (; ; )
                {
                    if (thQueue.ThreadState == System.Threading.ThreadState.Stopped)
                        break;
                    Thread.Sleep(50);
                }

                // wait for Worker thread(s) to stop
                // ToDo:

                // launch shutdown script
                lock (Console.Out)
                {
                    string sLog = "";
                    foreach (string sTmp in MemoryLog)
                    {
                        sLog += sTmp + "\n";
                    }
                    string sTempResults = QueueWorkerClass.MXRunScript("shutdown()", "", sLog);
                    if (sTempResults != "done")
                    {
                        Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " Warning: Problems while executing script 'shutdown()', Error msg = {0}", sTempResults);
                    }
                }
                
                App.EndBanner();

                if (!App.MXUnLockPid())
                {
                    // Hmm.. Can't unlock pid, but who was the initial locker then? :)
                    Console.WriteLine("Warning: Can't unlock pid!");
                }

            }

            if (bRestart)
            {
                return (-1);
            }
            else
            {
                return (0);
            }

        }

        // Define the event handlers.
        public static void OnChanged(object source, FileSystemEventArgs e)
        {
            if (!App.MXFileLocked("settings.xml")) {
                bool bSuccess = LoadSettings();
                string sTempResults = QueueWorkerClass.MXRunScript("settingschanged()", "", "");
                if (sTempResults != "done")
                {
                    Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " Warning: Problems while executing script 'settingschanged()', Error msg = {0}", sTempResults);
                }
            }
        }

        public static bool LoadSettings()
        {
            
            // read settings from xml file
            XmlDocument XmlDoc = new XmlDocument();
            try
            {
                XmlDoc.Load(cApp.settings.sIniFile);
                cApp.settings.sSettingsXml = XmlDoc.InnerXml.ToString();
            }
            catch (Exception ex)
            {
                // Ini .xml file not found. Quit?
                Console.WriteLine("FATAL ERROR: Exception while loading settings.xml: {0}", ex.ToString());
                return (false);
            }

            if (XmlDoc != null)
            {
                cApp.settings.iScanDelay = int.Parse(App.XmlGetProp(XmlDoc, "scandelay"));
                cApp.settings.iTimeOut = int.Parse(App.XmlGetProp(XmlDoc, "timeout"));
                cApp.settings.iWaitTime = int.Parse(App.XmlGetProp(XmlDoc, "wait_time"));
                cApp.settings.iIgnoreTime = int.Parse(App.XmlGetProp(XmlDoc, "ignore_time"));
            }
            else
            {
                // set default values
                cApp.settings.iScanDelay = 0; // use system events
                cApp.settings.iTimeOut = 120;
                cApp.settings.iWaitTime = 5;
                cApp.settings.iIgnoreTime = 60;
            }

            XmlNodeList nodes, nodes2;

            // read plugins into memory
            nodes = XmlDoc.SelectNodes("settings/plugins/plugin");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    bool bError = false;
                    string sPluginName = "";
                    string sPluginDescription = "";
                    string sPluginType = "";
                    int iPluginMaxWorktime = -1;
                    try { sPluginName = node.Attributes.GetNamedItem("name").Value; }
                    catch (Exception) { /* Warning: Plugin name missing */ }
                    try { sPluginDescription = node.Attributes.GetNamedItem("description").Value; }
                    catch (Exception) { /* Warning: Plugin name missing */ }
                    try { iPluginMaxWorktime = int.Parse(node.Attributes.GetNamedItem("max_worktime").Value); }
                    catch (Exception) { /* Warning: Plugin maxworktime missing */ }
                    try { sPluginType = node.Attributes.GetNamedItem("type").Value; }
                    catch (Exception)
                    {
                        Console.Error.WriteLine("Fatal Error: No plugin type specified in 'settings.xml'!");
                        bError = true;
                    }
                    if (!bError)
                    {
                        // try to initialize plugin
                        try
                        {
                            object plugObject = Activator.CreateInstance(Type.GetType(sPluginType));
                            //Cast this to an IPlugin interface and add to the collection
                            IPlugin plugin = (IPlugin)plugObject;
                            if (sPluginName != "") plugin.Name = sPluginName;
                            if (sPluginDescription != "") plugin.Description = sPluginDescription;
                            if (iPluginMaxWorktime != -1) plugin.MaxWorktime = iPluginMaxWorktime;
                            App.m_plugins.Add(plugin);
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine("Exception: {0}", ex.ToString());
                        }
                    }
                }
            }

            // read jobs into memory
            nodes = XmlDoc.SelectNodes("settings/jobs/job");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    try { cApp.settings.aFoldersPath.Add(node.Attributes.GetNamedItem("path").Value); }
                    catch (Exception) { /* Fatal Error: Must specify path for job */ }
                    try { cApp.settings.aFoldersName.Add(node.Attributes.GetNamedItem("name").Value); }
                    catch (Exception) { /* Fatal Error: Must specify name for job */ }
                    try { cApp.settings.aFoldersSubfolders.Add(node.Attributes.GetNamedItem("subfolders").Value); }
                    catch (Exception)
                    {
                        /* Default: No subfolders */
                        cApp.settings.aFoldersSubfolders.Add("false");
                    }
                    try { cApp.settings.aFoldersFilter.Add(node.Attributes.GetNamedItem("filter").Value); }
                    catch (Exception)
                    {
                        /* Default: No filter */
                        cApp.settings.aFoldersFilter.Add("");
                    }
                    try { cApp.settings.aFoldersRequireXml.Add(node.Attributes.GetNamedItem("require_xml").Value.ToLower()); }
                    catch (Exception)
                    {
                        /* Default: xml not required for companion */
                        cApp.settings.aFoldersRequireXml.Add("false");
                    }
                    try { cApp.settings.aFoldersRequireExt.Add(node.Attributes.GetNamedItem("require_ext").Value); }
                    catch (Exception)
                    {
                        /* Default: no required extension set */
                        cApp.settings.aFoldersRequireExt.Add("");
                    }
                    try { cApp.settings.aFoldersWaitTime.Add(node.Attributes.GetNamedItem("wait_time").Value); }
                    catch (Exception)
                    {
                        /* Default: one second wait time */
                        cApp.settings.aFoldersWaitTime.Add(cApp.settings.iWaitTime.ToString());
                    }
                    try { cApp.settings.aFoldersIgnoreTime.Add(node.Attributes.GetNamedItem("ignore_time").Value); }
                    catch (Exception)
                    {
                        /* Default: no ignore time set */
                        cApp.settings.aFoldersIgnoreTime.Add(cApp.settings.iIgnoreTime.ToString());
                    }
                    try { cApp.settings.aFoldersXml.Add(node.OuterXml.ToString()); }
                    catch (Exception)
                    {
                        /* Fatal Error: No commands for job */
                        cApp.settings.aFoldersXml.Add("");
                        Console.Error.WriteLine("FATAL ERROR: No commands for job");
                    }
                    Hashtable newTable = new Hashtable();
                    Hashtable replaceTable = new Hashtable();
                    Hashtable flagsTable = new Hashtable();
                    nodes2 = node.SelectNodes("parameters/parameter");
                    foreach (XmlNode node2 in nodes2)
                    {
                        try
                        {
                            /* Get name & value keypairs into hashtable */
                            string sName = node2.Attributes.GetNamedItem("name").Value;
                            string sValue = node2.Attributes.GetNamedItem("value").Value;
                            string sReplace = "";
                            string sWith = "";
                            string sFlags = "";
                            try { sReplace = node2.Attributes.GetNamedItem("replace").Value; }
                            catch { }
                            try { sWith = node2.Attributes.GetNamedItem("with").Value; }
                            catch { }
                            try { sFlags = node2.Attributes.GetNamedItem("flags").Value; }
                            catch { }
                            newTable.Add(sName, sValue);
                            replaceTable.Add(sName, String.Concat(sReplace, "|", sWith));
                            flagsTable.Add(sName, sFlags);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    cApp.settings.aFoldersParameters.Add(newTable);
                    cApp.settings.aFoldersParametersReplace.Add(replaceTable);
                    cApp.settings.aFoldersParametersFlags.Add(flagsTable);
                }
            }
            return (true);
        }        
    }
}
