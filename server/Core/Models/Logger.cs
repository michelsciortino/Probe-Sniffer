using System;
using System.Collections.Generic;
using System.Threading;

namespace Core.Models
{
    public static class Logger
    {
        #region Private Members
        private static Queue<string> log = null;
        private static Mutex logMutex = null;
        #endregion
        
        static Logger()
        {
            log = new Queue<string>();
            logMutex = new Mutex();
        }

        public static void Log(string message)
        {
            logMutex.WaitOne();
            try
            {
                log.Enqueue("[" + DateTime.Now.ToShortTimeString() + "] " + message);
            }
            catch
            {
                log = new Queue<string>();
            }
            logMutex.ReleaseMutex();
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
    }
}
