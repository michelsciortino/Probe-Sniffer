using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Server.ViewModels
{
    public class LogViewModel:Core.ViewModelBase.BaseViewModel
    {
        #region Private Members
        private string _logText = "";
        private Thread updater = null;
        #endregion

        #region Pubblic properties
        public CancellationTokenSource CancTokenSource;
        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                OnPropertyChanged(nameof(LogText));
            }
        }
        #endregion
        
       
        public LogViewModel()
        {
            Queue<string> history = Core.Models.Logger.GetLogHistory();
            while (history.Count > 0) _logText+= "\n" + history.Dequeue();
            CancTokenSource = new CancellationTokenSource();
        }

        public void StartLogging()
        {
            if (CancTokenSource.Token.IsCancellationRequested)
                return;
            if (updater is null)
                updater = new Thread(() => UpdateLog());
            if (updater.IsAlive is false)
                updater.Start();
        }


        public void UpdateLog()
        {
            while (CancTokenSource.IsCancellationRequested is false)
            {
                Thread.Sleep(500);
                Queue<string> newMessages = Core.Models.Logger.GetLogMessages();
                while (newMessages.Count > 0) _logText+= newMessages.Dequeue();
                OnPropertyChanged(nameof(LogText));
            }
        }
    }
}
