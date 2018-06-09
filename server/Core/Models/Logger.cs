using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Models
{
    public static class Logger
    {
        #region Private Members
        private static Queue<string> log = null;
        private static List<string> historyLog=null;
        private static Mutex logMutex = null;

        #endregion

        public static AutoResetEvent NewLogMessage = null;

        static Logger()
        {
            log = new Queue<string>();
            historyLog = new List<string>();
            logMutex = new Mutex();
            NewLogMessage = new AutoResetEvent(false);
        }

        public static void Log(string message)
        {
            logMutex.WaitOne();
            try
            {
                string entry = "[" + DateTime.Now.ToShortTimeString() + "] " + message;
                log.Enqueue(entry);
                historyLog.Add(entry);
            }
            catch { log = new Queue<string>(); historyLog = new List<string>(); }
            logMutex.ReleaseMutex();
            NewLogMessage.Set();
        }

        public static Queue<string> GetLogMessages()
        {
            Queue<string> ret = new Queue<string>();
            logMutex.WaitOne();
            try
            {
                while (log.Count > 0) ret.Enqueue(log.Dequeue());

            }
            catch
            {
                log = new Queue<string>();
            }
            logMutex.ReleaseMutex();
            return ret;
        }

        public static Queue<string> GetLogHistory()
        {
            Queue<string> ret = new Queue<string>();
            logMutex.WaitOne();
            foreach (var entry in historyLog) ret.Enqueue(entry);
            log.Clear();
            logMutex.ReleaseMutex();
            return ret;
        }
    }
}
