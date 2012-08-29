using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections;

namespace MediaMachineApp
{
	/// <summary>
	/// Summary description for Monitor.
	/// </summary>
	public class Monitor
	{
		public static App cApp;
        public static QueueWorker cQueueWorkerClass;

		public ArrayList Watchers = new ArrayList();

		public Monitor(ref App x, ref QueueWorker y)
		{
			cApp = x;
            cQueueWorkerClass = y;
		}

		public void Main()
		{

			int iCount = 0;
			for (int idx=0; idx<cApp.settings.aFoldersPath.Count; idx++)
			{
				// foreach .. string sFolder in cApp.settings.aFoldersPath)
				string sFolder = cApp.settings.aFoldersPath[idx].ToString();
				string sName = cApp.settings.aFoldersName[idx].ToString();
                string sFilter = cApp.settings.aFoldersFilter[idx].ToString();
                string sRequireXML = cApp.settings.aFoldersRequireXml[idx].ToString();
                string sRequireEXT = cApp.settings.aFoldersRequireExt[idx].ToString();
                int sWaitTime = Int32.Parse(cApp.settings.aFoldersWaitTime[idx].ToString());
                int sIgnoreTime = Int32.Parse(cApp.settings.aFoldersIgnoreTime[idx].ToString());
                if (sFilter == "") sFilter = "*.*";

                if (Directory.Exists(sFolder)) {
                    if ( cApp.settings.iScanDelay == 0 )
                    {
                        // Use system events
                        FileSystemWatcher FolderWatcher = new FileSystemWatcher();
                        try
                        {
                            FolderWatcher.Path = sFolder;
                            // Watch for changes in LastAccess and LastWrite times, and
                            // the renaming of files or directories. 
                            // FolderWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
                            // NotifyFilters.DirectoryName;

                            if (sFilter != "") FolderWatcher.Filter = sFilter;
                            if (cApp.settings.aFoldersSubfolders[idx].ToString().ToLower() == "yes")
                            {
                                FolderWatcher.IncludeSubdirectories = true;
                            }
                            else
                            {
                                FolderWatcher.IncludeSubdirectories = false;
                            }
                            FolderWatcher.Created += new FileSystemEventHandler(OnChanged);
                            FolderWatcher.Changed += new FileSystemEventHandler(OnChanged);
                            FolderWatcher.Deleted += new FileSystemEventHandler(OnChanged);
                            FolderWatcher.Renamed += new RenamedEventHandler(OnRenamed);
                            FolderWatcher.NotifyFilter = System.IO.NotifyFilters.DirectoryName | System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.Size | System.IO.NotifyFilters.LastWrite;
                            FolderWatcher.EnableRaisingEvents = true;
                            // activate folder watcher
                            Watchers.Add(FolderWatcher);
                            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - Added new job as '{0}'", sName);
                            string sFolderDisp;
                            if (sFolder.Length > 56)
                                sFolderDisp = ".." + sFolder.Substring(sFolder.Length - 54);
                            else
                                sFolderDisp = sFolder;

                            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - [ folder={0} ]", sFolderDisp);
                            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - [ wait_time={0}s, ignore_time={1}s ]", sWaitTime, sIgnoreTime);
                            Console.Write(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - [ filter: {0}", sFilter);
                            if (sRequireXML.ToLower().Contains("true"))
                                Console.Write(", xml-required");
                            if (sRequireEXT != "")
                                Console.Write(", extensions: {0}", sRequireEXT);
                            Console.WriteLine(" ]");

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - Catched Exception: {0}", e.ToString());
                            FolderWatcher.Dispose();
                        }
                    }
                    else
                    {
                        // Use timers
                        FileSystemWatcherWithTimer FolderWatcher = new FileSystemWatcherWithTimer();
                        FolderWatcher.Path = sFolder;
                        if (sFilter != "") FolderWatcher.Filter = sFilter;
                        FolderWatcher.Created += new FileSystemWatcherWithTimer.FileSystemEventHandlerWT(OnChanged);
                        FolderWatcher.Changed += new FileSystemWatcherWithTimer.FileSystemEventHandlerWT(OnChanged);
                        FolderWatcher.Deleted += new FileSystemWatcherWithTimer.FileSystemEventHandlerWT(OnChanged);
                        FolderWatcher.ScanDelay = cApp.settings.iScanDelay;

                        // activate folder watcher
                        Watchers.Add(FolderWatcher);
                        Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - Added new job as '{0}'", sName);
                        string sFolderDisp;
                        if (sFolder.Length > 56)
                            sFolderDisp = ".." + sFolder.Substring(sFolder.Length - 54);
                        else
                            sFolderDisp = sFolder;

                        Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - [ folder={0} ]", sFolderDisp);
                        Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - [ wait_time={0}s, ignore_time={1}s ]", sWaitTime, sIgnoreTime);
                        Console.Write(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - [ filter: {0}", sFilter);
                        if (sRequireXML.ToLower().Contains("true"))
                            Console.Write(", xml-required");
                        if (sRequireEXT != "")
                            Console.Write(", extensions: {0}", sRequireEXT);
                        Console.WriteLine(" ]");

                    }
                    iCount++;
                }
			}
            Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - SYSTEM READY ( Write q or quit to quit. ? for help )");

			string oldTime = "";

            string oldStatus = "";
            string oldDay = DateTime.Now.ToString("yyMMdd");
            string oldHour = DateTime.Now.ToString("yyMMddHH");

			for( ; ; )
			{
                int cIgnored = 0;
                int cWaiting = 0;
                int cHalfReady = 0;
                int cReady = 0;
                int cDone = 0;
                for (int i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                {
                    switch ((int)cApp.queue.aQueueStatus[i])
                    {
                        case 0:
                            cWaiting++;
                            break;
                        case -1:
                            cIgnored++;
                            break;
                        case 1:
                            cHalfReady++;
                            break;
                        case 2:
                            cReady++;
                            break;
                        case 3:
                            cDone++;
                            break;
                        default:
                            break;
                    }
                }

                string sStatus = string.Format("Waiting {0} | Ignored {1} | Ready {2}/{3} | Queue {4}", cWaiting, cIgnored, cHalfReady, cReady, cApp.queue.aQueueFileName.Count);

                if (oldHour != DateTime.Now.ToString("yyMMddHH"))
                {
                    oldHour = DateTime.Now.ToString("yyMMddHH");
                    // check if we have "Fatal Error" or "Warning" on the last log file
                    string sLog = "";
                    lock (Console.Out)
                    {
                        foreach (string sTmp in cApp.MemoryLog)
                        {
                            sLog += sTmp + "\n";
                        }
                    }
                    if (sLog.ToLower().Contains("fatal error"))
                    {
                        cQueueWorkerClass.MXRunScript("fatalerror()", "", sLog);
                        cApp.MemoryLog.Clear();
                    }
                    else
                    {
                        if (sLog.ToLower().Contains("warning"))
                        {
                            cQueueWorkerClass.MXRunScript("warning()", "", sLog);
                            cApp.MemoryLog.Clear();
                        }
                    }
                    
                    
                }

                if (oldDay != DateTime.Now.ToString("yyMMdd")) {
                    // launch daily script
                    string sLog = "";
                    lock (Console.Out)
                    {
                        Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                        Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " INFO - DAY CHANGED");

                        foreach (string sTmp in cApp.MemoryLog)
                        {
                            sLog += sTmp + "\n";
                        }
                    }
                    cQueueWorkerClass.MXRunScript("daily()", "", sLog);
                    cApp.MemoryLog.Clear();
                    oldDay = DateTime.Now.ToString("yyMMdd");
                }

				if ((oldTime != DateTime.Now.ToString("yyMMdd-HH:mm")) || (sStatus != oldStatus)) 
				{
					lock(Console.Error) 
					{
						Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
						Console.Error.Write("{0}{1} | {2} > ", '\x0d', DateTime.Now.ToString("yyMMdd-HH:mm"), sStatus);
					}
					oldTime = DateTime.Now.ToString("yyMMdd-HH:mm");
                    oldStatus = sStatus;
				}
				Thread.Sleep(1000);
				if (!cApp.bRunning) break;
			}
            if (cApp.settings.iScanDelay == 0)
            {
                // Use system events
                foreach (FileSystemWatcher FolderWatcher in Watchers)
                {
                    FolderWatcher.Dispose();
                }
            }
            else
            {
                // Use timers
                Watchers.Clear();
            }
		}	

        // Define the event handlers.
		public static void OnChanged(object source, FileSystemEventArgs e)
		{
            //Console.WriteLine("OnChanged:");
            //Console.WriteLine(" FullPath = {0}", e.FullPath);
            //Console.WriteLine(" Name = {0}", e.Name);
            // Selvitä onko kyseessä tiedosto vai kansio tms.
            FileInfo FileInf = new FileInfo(e.FullPath);
            if (!FileInf.Attributes.ToString().Contains("Directory"))
            {
                // Tiedosto on vaihtunut
                // 1.) löytyykö queue:sta ko. tiedosto? jos ei löydy lisätään
                // 2.) päivitä tiedoston aikaleima queue:ssa
                // 3.) jos tiedosto poistuu (esim siirretty) poista queuesta
                // find job by fullpath
                int jIdx = -1;
                for (int i = 0; i < cApp.settings.aFoldersPath.Count; i++)
                {
                    if (e.FullPath.StartsWith(cApp.settings.aFoldersPath[i].ToString()))
                    {
                        jIdx = i;
                        break;
                    }
                }
                
			    // Specify what is done when a file is changed, created, or deleted.
			    switch (e.ChangeType)
			    {
				    case System.IO.WatcherChangeTypes.Deleted:
					    lock (Console.Error) 
					    {
                            //if (jIdx != -1)
                            //{
                            //    // job found
                            //    cApp.queue.Add(e.FullPath, jIdx);
                            //    /*Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                            //    Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " >>> {0}, DELETED: {1}", cApp.settings.aFoldersName[jIdx], e.FullPath);*/
                            //}
                            //else
                            //{
                            //    // job not found. This should not be even possible :LOL:
                            //    // no idea to add because not in any job..
                            //    /*Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                            //    Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " >>> UNKNOWN JOB!!, DELETED: {0}", e.FullPath);*/
                            //}
                            cApp.queue.Remove(e.FullPath);
					    }
					    break;
				    case System.IO.WatcherChangeTypes.Changed:
					    lock (Console.Error) 
					    {
                            if (jIdx != -1)
                            {
                                // job found
                                cApp.queue.Add(e.FullPath, jIdx);
                                /*Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                                Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " >>> {0}, CHANGED: {1}", cApp.settings.aFoldersName[jIdx], e.FullPath);*/
                            }
                            else
                            {
                                // job not found. This should not be even possible :LOL:
                                // no idea to add because not in any job..
                                /*Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                                Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " >>> UNKNOWN JOB!!, CHANGED: {0}", e.FullPath);*/
                            }
                        }
					    break;
				    case System.IO.WatcherChangeTypes.Created:
					    lock (Console.Error) 
					    {
                            if (jIdx != -1)
                            {
                                // job found
                                cApp.queue.Add(e.FullPath, jIdx);
                                /*Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                                Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " >>> {0}, CREATED: {1}", cApp.settings.aFoldersName[jIdx], e.FullPath);*/
                            }
                            else
                            {
                                // job not found. This should not be even possible :LOL:
                                // no idea to add because not in any job..
                                /*Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                                Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " >>> UNKNOWN JOB!!, CREATED: {0}", e.FullPath);*/
                            }
					    }
					    break;
				    default:
					    break;
			    }
		    }
        }

		// Define the event handlers.
		public static void OnRenamed(object source, RenamedEventArgs e)
		{
            FileInfo FileInf = new FileInfo(e.FullPath);
            if (!FileInf.Attributes.ToString().Contains("Directory"))
            {
                int jIdx = -1;
                for (int i = 0; i < cApp.settings.aFoldersPath.Count; i++)
                {
                    if (e.OldFullPath.StartsWith(cApp.settings.aFoldersPath[i].ToString()))
                    {
                        jIdx = i;
                        break;
                    }
                }
                lock (Console.Error)
                {
                    if (jIdx != -1)
                    {
                        // job found
                        cApp.queue.Rename(e.OldFullPath, e.FullPath, jIdx);
                        /*Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                        Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " >>> {0}, RENAMED: {1} to {2}", cApp.settings.aFoldersName[jIdx], e.OldFullPath, e.FullPath);*/
                    }
                    else
                    {
                        // job not found. This should not be even possible :LOL:
                        // no idea to add because not in any job..
                        /*Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                        Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " >>> UNKNOWN JOB!!, RENAMED: {0} to {1}", e.OldFullPath, e.FullPath);*/
                    }
                }
            }
		}

	}

}
