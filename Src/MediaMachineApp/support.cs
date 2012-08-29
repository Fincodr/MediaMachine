using System;
using System.IO;
using System.Collections;

namespace MediaMachineApp
{
	/// <summary>
	/// 
	/// Class SplitWriter
	/// 
	/// Description:
	///  SplitWriter class extends normal console output to write
	/// to file at the same time.
	/// 
	/// </summary>
	class SplitWriter : StreamWriter
	{
		private TextWriter ConsoleOut;
        private ArrayList MemLog;

		public SplitWriter(string Path) : base(Path)
		{
			ConsoleOut = Console.Out;
			this.AutoFlush = true;
		}

		public SplitWriter(string Path, bool Append) : base(Path, Append)
		{
			ConsoleOut = Console.Out;
			this.AutoFlush = true;
		}

        public SplitWriter(string Path, bool Append, ref ArrayList MemLog): base(Path, Append)
        {
            ConsoleOut = Console.Out;
            this.AutoFlush = true;
            this.MemLog = MemLog;
        }

		public override void WriteLine(string value)
		{
			base.WriteLine (value);
			ConsoleOut.WriteLine(value);
			base.Flush();
            if (MemLog != null) MemLog.Add(value);
		}

		public override void Write(string value)
		{
			base.Write (value);
			ConsoleOut.Write(value);
			base.Flush();
            if (MemLog != null) MemLog[MemLog.Count-1] = MemLog[MemLog.Count-1] + value;
        }
	}
}
