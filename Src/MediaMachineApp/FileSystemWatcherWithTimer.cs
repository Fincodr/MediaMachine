using System;
using System.Collections;
using System.Text;
using System.Timers;
using System.IO;

namespace MediaMachineApp
{

    class FileSystemWatcherWithTimer
    {
        // Define a delegate named LogHandler, which will encapsulate
        // any method that takes a string as the parameter and returns no value
        public delegate void FileSystemEventHandlerWT(object source, FileSystemEventArgs e);

        // Define an Event based on the above Delegate
        public event FileSystemEventHandlerWT Created;
        public event FileSystemEventHandlerWT Changed;
        public event FileSystemEventHandlerWT Deleted;

        private Hashtable hFiles = new Hashtable();
        private Hashtable hFilesFound = new Hashtable();

        public string Filter;
        public string Path;
        private int iScanDelay;

        public int ScanDelay
        {
            get { return iScanDelay; }
            set {
                iScanDelay = value;
                timer.Interval = iScanDelay * 1000;
                timer.Start();
            }
        }

        private Timer timer;

        public FileSystemWatcherWithTimer()
        {
            timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler( OnTimerEvent );
        }

        public void OnTimerEvent( object source, ElapsedEventArgs e )
        {
            // Get files from specified folder and check what are the differences for last time, easy to do with hashtable
            DirectoryInfo di = new DirectoryInfo(Path);
            FileInfo[] rgFiles = di.GetFiles(Filter);
            // Set each file as not found on next iteration
            foreach (DictionaryEntry de in hFiles)
            {
                hFilesFound[de.Key] = false;
            }
            for (int i = 0; i != rgFiles.Length; ++i )
            {
                FileInfo fi = (FileInfo)rgFiles[i];
                string FileHash = fi.FullName;
                if ( hFiles.ContainsKey(FileHash) )
                {
                    // Already found, check the modify date
                    FileInfo prev = (FileInfo)hFiles[FileHash];
                    hFilesFound[FileHash] = true;
                    if ( prev.LastWriteTime != rgFiles[i].LastWriteTime )
                    {
                        // Modified file
                        FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Changed, fi.DirectoryName, fi.Name);
                        Changed(new object(), args);
                    }
                }
                else
                {
                    // New file
                    FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Created, fi.DirectoryName, fi.Name);
                    Created(new object(), args);
                    hFiles.Add(FileHash, fi);
                    hFilesFound.Add(FileHash, true);
                }
            }
            // Check for missing (deleted or moved) files
            foreach (DictionaryEntry de in hFiles)
            {
                string FileHash = (string)de.Key;
                if ( (bool)hFilesFound[FileHash] == false )
                {
                    // New file
                    FileInfo fi = (FileInfo)hFiles[FileHash];
                    FileSystemEventArgs args = new FileSystemEventArgs(WatcherChangeTypes.Deleted, fi.DirectoryName, fi.Name);
                    Deleted(new object(), args);
                    hFilesFound.Remove(FileHash);
                    hFiles.Remove(FileHash);
                }
            }

            // System events:
            //  FullPath = p:\production\in\kansi.pdf
            //  Name = kansi.pdf
        }

    }
}
