using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Ui
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string _pipeID;
        private NamedPipeClientStream _pipeUiStream;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 1){
                _pipeID = e.Args[0];
            }
            
            StartIPC();

            Exit += App_Exit;
            SendPipeMessage("Showing main window");
            this.MainWindow = new DataVisualizationWindow();
            MainWindow.Show();


        }

        private bool StartIPC()
        {
            if (_pipeID == null) return false;
            System.Diagnostics.Trace.WriteLine("Starting IPC client for " + _pipeID);
            _pipeUiStream = new NamedPipeClientStream(".",
                                                      _pipeID,
                                                      PipeDirection.InOut,
                                                      PipeOptions.Asynchronous);

            try
            {
                _pipeUiStream.Connect(3000);
                System.Diagnostics.Trace.WriteLine("Connected to IPC server. " + _pipeID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("StartIPC Error for " + ex.ToString());
                return false;
            }
            return true;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            SendPipeMessage("exiting");
            System.Diagnostics.Trace.WriteLine("Application Exit " + _pipeID);
        }


        private void SendPipeMessage(string message)
        {
            if (_pipeID == null) return;
            try
            {
                if (_pipeUiStream.IsConnected)
                {
                    StreamWriter sw = new StreamWriter(_pipeUiStream);
                    sw.WriteLine(message);
                    sw.Flush();
                }
            }
            catch { }
        }
    }
}
