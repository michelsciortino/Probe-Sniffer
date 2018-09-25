using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Child process lifetime manager
    /// </summary>
    public class ChildProcessHost : IDisposable
    {
        public delegate void Handler(object sender);
        public event Handler Child_exited;

        public const int BUFFER_SIZE = 1024 * 2;
        public bool Running { get; private set; }
        private readonly string _hostProgramName=null;
        private string _pipeID;
        private Process _childProcess;
        private NamedPipeServerStream _pipeServerStream;
        private bool _isDisposing;
        private Thread _pipeMessagingThread;

        /// <summary>
        /// Constructor
        /// </summary>
        public ChildProcessHost(string HostProgramName)
        {
            this._hostProgramName = HostProgramName;
            this.Running = false;
        }

        /// <summary>
        /// Starts the IPC server and run the child process
        /// </summary>
        /// <param name="paramUID">Unique ID of the named pipe (null if pipe is not required)</param>
        /// <returns></returns>
        public bool Start(string paramUID = null)
        {
            ProcessStartInfo processInfo;
            if (paramUID != null)
            {
                _pipeID = paramUID;
                _pipeMessagingThread = new Thread(new ThreadStart(StartIPCServer))
                {
                    Name = this.GetType().Name + ".PipeMessagingThread",
                    IsBackground = true
                };
                _pipeMessagingThread.Start();
                processInfo = new ProcessStartInfo(_hostProgramName, _pipeID);
            }
            else
                processInfo = new ProcessStartInfo(_hostProgramName);

            //Starting Child Process
            try
            {
                _childProcess = Process.Start(processInfo);
            }
            catch(Exception ex)
            {
                Console.WriteLine(string.Format("Failed to start {0}.exe", _hostProgramName));
                return false;
            }
            Running = true;
            return true;
        }

        /// <summary>
        /// Start the IPC server listener and wait for
        /// incomming messages from the appropriate child process
        /// </summary>
        void StartIPCServer()
        {
            if (_pipeServerStream == null)
            {
                _pipeServerStream = new NamedPipeServerStream(_pipeID,
                                                              PipeDirection.InOut,
                                                              1,
                                                              PipeTransmissionMode.Byte,
                                                              PipeOptions.Asynchronous,
                                                              BUFFER_SIZE,
                                                              BUFFER_SIZE);

            }

            // Wait for a client to connect
            Console.WriteLine(string.Format("{0}:Waiting for child process connection...", _pipeID));
            try
            {
                //Wait for connection from the child process
                _pipeServerStream.WaitForConnection();
                Console.WriteLine(string.Format("Child process {0} is connected.", _pipeID));
            }
            catch (ObjectDisposedException exDisposed)
            {
                Console.WriteLine(string.Format("StartIPCServer for process {0} error: {1}", this._pipeID, exDisposed.Message));
            }
            catch (IOException exIO)
            {
                Console.WriteLine(string.Format("StartIPCServer for process {0} error: {1}", this._pipeID, exIO.Message));
            }

            bool retRead = true; ;
            while (retRead && !_isDisposing)
            {
                retRead = StartAsyncReceive();
                Thread.Sleep(30);
            }            
            Running = false;
            _pipeServerStream.Close();
            _pipeServerStream = null;
            Child_exited(Running);
        }

        /// <summary>
        /// Read line of text from the connected client
        /// </summary>
        /// <returns>return false on pipe communication exception</returns>
        bool StartAsyncReceive()
        {
            StreamReader sr = new StreamReader(_pipeServerStream);
            try
            {
                string str = sr.ReadLine();

                if (string.IsNullOrEmpty(str))
                {
                    // Child Process is down
                    Console.WriteLine(string.Format("{0} exited", _hostProgramName));
                    return false;
                }
                Console.WriteLine(string.Format("{0}: Received: {1}. (Thread {2})", _pipeID, str, Thread.CurrentThread.ManagedThreadId));
            }
            // Catch the IOException that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                Console.WriteLine("AsyncReceive ERROR: {0}", e.Message);
                return false;
            }
            return true;
        }

        void DisposeChildProcess()
        {
            try
            {
                _isDisposing = true;
                try
                {
                    //It will fails if the process doesn't exist
                    _childProcess.Kill();
                    Console.WriteLine(string.Format("{0} killed", _hostProgramName));
                }
                catch { }

                //This will stop any pipe activity
                if (_pipeID != null)
                {
                    _pipeServerStream.Dispose();
                    Console.WriteLine(string.Format("{0} Process pipe {1} is Closed", _hostProgramName, _pipeID));
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0} exited with error: {1}", _hostProgramName, ex.Message));
            }
            Running = false;
        }

        #region IDisposable Members
        public void Dispose()
        {
            DisposeChildProcess();
        }
        #endregion
    }
}
