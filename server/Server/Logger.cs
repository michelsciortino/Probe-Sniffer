using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using Core.Models;
using Server.ViewModels;
using Server.Windows;

namespace Server
{
    public class Logger
    {
        private enum LogWindowState
        {
            CLOSED = 0,
            OPEN,
        }
        
        #region Private
        private CancellationTokenSource _cancel = null;
        private Thread _loggerUiThread = null;
        private LogWindow _logWindow = null;
        private LogViewModel _logvm = null;
        private LogWindowState state = LogWindowState.CLOSED;
        #endregion

        #region Constructor
        public Logger()
        {
            _logvm = new LogViewModel();
            _logvm.StartLogging();
        }
        
        #endregion

        #region Public Methods
        public void Show()
        {
            if (state == LogWindowState.CLOSED)
            {
                if (_loggerUiThread != null)
                {
                    Dispatcher.FromThread(_loggerUiThread)?.InvokeShutdown();
                    _loggerUiThread.Join();
                }
                _loggerUiThread = new Thread(() => {
                    
                    _logWindow = new LogWindow(_logvm);
                    _logWindow.Show();
                    _logWindow.Closed += _logWindow_Closed;
                    try { Dispatcher.Run(); }
                    catch { return; }
                    return;
                });
                _loggerUiThread.SetApartmentState(ApartmentState.STA);
                _loggerUiThread.IsBackground = false;
                _loggerUiThread.Start();
                while (true)
                {
                    Dispatcher dispatcher=Dispatcher.FromThread(_loggerUiThread);
                    if (dispatcher == null) Thread.Sleep(100);
                    else break;
                }                
            }
            else
            {
                Dispatcher dispatcher= Dispatcher.FromThread(_loggerUiThread);
                if (dispatcher is null) return;
                if (!dispatcher.HasShutdownStarted) dispatcher.Invoke(() => _logWindow.Activate());
            }
            state = LogWindowState.OPEN;
        }

        private void _logWindow_Closed(object sender, EventArgs e)
        {
            state = LogWindowState.CLOSED;
        }

        public void Close()
        {
            _cancel.Cancel();
            if (_loggerUiThread != null)
            {
                Dispatcher.FromThread(_loggerUiThread)?.InvokeShutdown();
                _loggerUiThread.Join();
                _loggerUiThread = null;
            }
        }
        #endregion
    }
}
