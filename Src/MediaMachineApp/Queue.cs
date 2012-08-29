using System;
using System.Threading;
using System.Collections;

namespace MediaMachineApp
{
	/// <summary>
	/// Summary description for Queue.
	/// </summary>
	public class Queue
	{
		private App cApp;

		public Queue(ref App x)
		{
			cApp = x;
		}

        public void QueueUpdate()
        {
            lock (cApp.queue.aQueueFileName)
            {
                if (cApp.queue.aQueueFileName.Count > 0)
                {
                    int i, iStatus;
                    DateTime dFrom, dTo;
                    TimeSpan tSpan;
                    string sRequireXml;
                    // go thru queue and check which file should be moved to processing, ignored or terminated.
                    for (i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                    {
                        int jIdx = (int)cApp.queue.aQueueJobIdx[i];
                        int sWaitTime = Int32.Parse(cApp.settings.aFoldersWaitTime[jIdx].ToString());
                        int sIgnoreTime = Int32.Parse(cApp.settings.aFoldersIgnoreTime[jIdx].ToString());                        
                        dFrom = DateTime.Now;
                        dTo = (DateTime)cApp.queue.aQueueTimeStamp[i];
                        tSpan = dFrom.Subtract(dTo);
                        cApp.queue.aQueueElapsed[i] = (double)tSpan.TotalSeconds;
                    }
                }
            }
        }

        /// <summary>
        /// QueueFind
        /// </summary>
        /// <param name="sFileName"></param>
        /// <returns></returns>
        public int QueueFind(string sFileName)
        {
            // find file from queue and return status
            lock (cApp.queue.aQueueFileName)
            {
                for (int i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                {
                    if (cApp.queue.aQueueFileName[i].Equals(sFileName))
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

        public void QueueGetJobs()
        {
            lock (cApp.queue.aQueueFileName)
            {
                if (cApp.queue.aQueueFileName.Count > 0)
                {
                    // go thru queue and check which file should be moved to processing, ignored or terminated.
                    for (int i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                    {
                        int iIdx;
                        int jIdx = (int)cApp.queue.aQueueJobIdx[i];
                        int sWaitTime = Int32.Parse(cApp.settings.aFoldersWaitTime[jIdx].ToString());
                        int sIgnoreTime = Int32.Parse(cApp.settings.aFoldersIgnoreTime[jIdx].ToString());
                        string sFileFullName = cApp.queue.aQueueFilePath[i].ToString();
                        string sFileName = cApp.queue.aQueueFileName[i].ToString();
                        double dElapsed = (double)cApp.queue.aQueueElapsed[i];
                        int iStatus = (int)cApp.queue.aQueueStatus[i];
                        string sRequireXml = cApp.settings.aFoldersRequireXml[jIdx].ToString();
                        // Get parameters from hashtable
                        Hashtable aParameters = (Hashtable)cApp.settings.aFoldersParameters[jIdx];
                        Hashtable aParametersReplace = (Hashtable)cApp.settings.aFoldersParametersReplace[jIdx];
                        string sTargetPath, sErrorPath;
                        //try { sTargetPath = aParameters["target.path"].ToString(); } catch { sTargetPath = ""; }
                        //try { sErrorPath = aParameters["error.path"].ToString(); } catch { sErrorPath = ""; }
                        sTargetPath = App.MXGetParam(aParameters, aParametersReplace, "target.path");
                        sErrorPath = App.MXGetParam(aParameters, aParametersReplace, "error.path");
                        // Update file status
                        if (iStatus == 0)
                        {
                            if (dElapsed > sWaitTime)
                            {
                                // is the file locked?
                                if (!App.MXFileLocked(sFileFullName))
                                {
                                    // no, is the file a companion (xml-file)?
                                    if (sFileName.EndsWith(".xml"))
                                    {
                                        //Console.Error.WriteLine("Notice: Xml-File {0} detected, now we are going to find any pdf file with status=1 and set to status=0 for both for recheck?", sFileName);
                                        // Xml file, if xml is required for this job we should now
                                        // recheck every companion file (by setting elapsedtime to zero or similar)
                                        bool bFound = false;
                                        string sFileOnly = App.MXGetFileNameWithoutExtension(App.MXGetOnlyFileName(sFileName));
                                        for (int j = 0; j < cApp.queue.aQueueFileName.Count; j++)
                                        {
                                            string sOneFile = App.MXGetFileNameWithoutExtension(App.MXGetOnlyFileName(cApp.queue.aQueueFileName[j].ToString()));
                                            if ((sFileOnly == sOneFile) && (i != j))
                                            {
                                                // is the file ready to be processed?
                                                if ((int)cApp.queue.aQueueStatus[j] == 1)
                                                {
                                                    // yes, but maybe the companion xml file is not known so set to zero to recheck status.
                                                    //Console.Error.WriteLine("Notice: File {0} detected for {1}, setting to 0 for recheck?", cApp.queue.aQueueFileName[j].ToString(), cApp.queue.aQueueFileName[i].ToString());
                                                    //Console.Error.WriteLine("{0} == {1}", sFileOnly, sOneFile);
                                                    cApp.queue.aQueueStatus[j] = 0;
                                                    cApp.queue.aQueueTimeStamp[i] = DateTime.Now;
                                                    cApp.queue.aQueueTimeStamp[j] = DateTime.Now;
                                                    bFound = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (!bFound)
                                        {
                                            // We did have .xml file but we do not have companion file for it
                                            // so let's just wait with the xml file
                                        }
                                    }
                                    // is the file the right type/extension?
                                    string sRequireExt = cApp.settings.aFoldersRequireExt[jIdx].ToString();
                                    string[] sRequireExts = sRequireExt.ToLower().Split(',');
                                    bool bExtOk = false;
                                    if (sRequireExt != "")
                                    {
                                        foreach (string s in sRequireExts)
                                        {
                                            if (sFileName.EndsWith("." + s))
                                            {
                                                // accepted extension, just break out.
                                                bExtOk = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        bExtOk = true;
                                    }
                                    if (bExtOk)
                                    {
                                        // file can be opened without problems
                                        // file is ready to be processed
                                        cApp.queue.aQueueStatus[i] = 1;
                                        // do we require xml ?
                                        if (sRequireXml.Equals("true"))
                                        {
                                            // scan thru every queued file that would be ready for processing #
                                            iIdx = QueueFindXML(sFileName);
                                            if (iIdx != -1)
                                            {
                                                // found "companion" xml file so we should set both for processing
                                                cApp.queue.aQueueStatus[i] = 2;
                                                cApp.queue.aQueueStatus[iIdx] = 2;
                                                Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                                                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " q +{0} [for {1}]", sFileName, cApp.settings.aFoldersName[jIdx]);
                                            }
                                        }
                                        else
                                        {
                                            // send to processing
                                            cApp.queue.aQueueStatus[i] = 2;
                                            Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                                            Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " q +{0} [for {1}]", sFileName, cApp.settings.aFoldersName[jIdx]);
                                        }
                                    }
                                    else
                                    {
                                        // ignored file but can be still be needed (for example as an xml companion)
                                        cApp.queue.aQueueStatus[i] = 1;
                                    }
                                }
                                else
                                {
                                    Console.Error.WriteLine("FILE LOCKED = {0}", sFileFullName);
                                    // file is not yet ready, give 5 more seconds and then try again
                                    DateTime dNew;
                                    dNew = (DateTime)cApp.queue.aQueueTimeStamp[i];
                                    dNew += TimeSpan.FromSeconds(5);
                                    cApp.queue.aQueueTimeStamp[i] = dNew; // .Add(DateTime.Now);
                                }

                            }
                        }
                        // any file that is not processed or is idle more than the ignoredtime will be ignored (i.e. sent to error folder)
                        if ((iStatus == -1) || (iStatus == 0) || (iStatus == 1))
                        {
                            if ((dElapsed > sIgnoreTime) || (iStatus == -1))
                            {
                                // send to ignored
                                cApp.queue.aQueueStatus[i] = -99;
                                Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
                                Console.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm") + " q Warning: File {0} ignored [from {1}]", sFileName, cApp.settings.aFoldersName[jIdx]);
                                App.MXMoveFile(sFileFullName, sErrorPath + "\\" + sFileName, false);
                            }
                        }
                        // any file that is processed and is over ignore time will be send to possible out folder
                        if ((iStatus == 3))
                        {
                            if (sTargetPath != "")
                            {
                                // send to target.path
                                cApp.queue.aQueueStatus[i] = -99;
                                App.MXMoveFile(sFileFullName, sTargetPath + "\\" + sFileName, false);
                            }
                        }
                    }
                }
            }
        }

        public void QueueOutput()
        {
            /*
            lock (cApp.queue.aQueueFileName)
            {

                bool bMultiline = false;
                if (cApp.queue.aQueueFileName.Count > 1)
                {
                    bMultiline = true;
                    Console.Error.WriteLine("-------------------------------------------------------------------------");
                }
                for (int i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                {
                    int jIdx = (int)cApp.queue.aQueueJobIdx[i];
                    if (bMultiline)
                        Console.Error.WriteLine("{0} | {1} | {2} | {3}", cApp.settings.aFoldersName[jIdx].ToString().PadRight(12), cApp.queue.aQueueStatus[i], cApp.queue.aQueueElapsed[i], cApp.queue.aQueueFileName[i].ToString());
                    else
                        Console.Error.Write("{0} | {1} | {2} | {3}", cApp.settings.aFoldersName[jIdx].ToString().PadRight(12), cApp.queue.aQueueStatus[i], cApp.queue.aQueueElapsed[i], cApp.queue.aQueueFileName[i].ToString());
                }
            }
            */
        }

		public void Main()
		{

			for( ; ; )
			{
				Thread.Sleep(250);
				if (!cApp.bRunning) break;
                QueueUpdate();
                QueueGetJobs();
                QueueOutput();

                /*
                lock (cApp.queue.aQueueFileName)
                {
                    if (cApp.queue.aQueueFileName.Count > 0)
                    {
                        int i, iStatus;
                        DateTime dFrom, dTo;
                        TimeSpan tSpan;
                        string sRequireXml;
                        // go thru queue and check which file should be moved to processing, ignored or terminated.
                        for (i = 0; i < cApp.queue.aQueueFileName.Count; i++)
                        {
                            int jIdx = (int)cApp.queue.aQueueJobIdx[i];
                            int sWaitTime = Int32.Parse(cApp.settings.aFoldersWaitTime[jIdx].ToString());
                            int sIgnoreTime = Int32.Parse(cApp.settings.aFoldersIgnoreTime[jIdx].ToString());
                            dFrom = DateTime.Now;
                            dTo = (DateTime)cApp.queue.aQueueTimeStamp[i];
                            tSpan = dFrom.Subtract(dTo);
                            iStatus = (int)cApp.queue.aQueueStatus[i];
                            sRequireXml = cApp.settings.aFoldersRequireXml[jIdx].ToString();
                            if (iStatus == 0)
                            {
                                if ((int)tSpan.Seconds > sWaitTime)
                                {
                                    // file is ready to be processed
                                    cApp.queue.aQueueStatus[i] = 1;
                                    // do we require xml ?
                                    if (sRequireXml.Equals("yes"))
                                    {
                                        // scan thru every queued file that would be ready for processing #
                                        
                                    }
                                    else
                                    {
                                        // send to processing
                                        cApp.queue.aQueueStatus[i] = 2;
                                    }
                                }
                                if ((int)tSpan.Seconds > sIgnoreTime)
                                {
                                    // send to ignored
                                    cApp.queue.aQueueStatus[i] = -1;
                                }
                            }
                        }
                        
                    }
                }
                */
				/*
				lock(Console.Error) 
				{
					Console.Error.Write("{0}                                                                              {1}", '\x0d', '\x0d');
					Console.Error.WriteLine(DateTime.Now.ToString("yyMMdd-HH:mm:ss") + " QUEUE: Hearbeat");
				}
				*/
			}
		}
	}
}
