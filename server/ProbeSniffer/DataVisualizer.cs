using Core.Models;
using ProbeSniffer.ViewModels.DataVisualizer;
using ProbeSniffer.Windows;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ProbeSniffer
{
    public class DataVisualizer
    {
        private enum VisualizerState
        {
            CLOSED = 0,
            OPEN,
            HIDDEN
        }

        #region Private
        private Thread VisualizerThread = null;
        private DataVisualizationWindow _dataVisualizationWindow = null;
        private List<Device> _devices=null;
        private VisualizerState state = VisualizerState.CLOSED;
        #endregion

        #region Constructor

        public DataVisualizer(IList<Device> devices)
        {
            _devices = devices as List<Device>;
        }
        #endregion

        #region Public Methods
        public void Show()
        {
            if (state == VisualizerState.CLOSED)
            {
                if (VisualizerThread != null)
                {
                    Dispatcher.FromThread(VisualizerThread)?.InvokeShutdown();
                    VisualizerThread.Join();
                }
                VisualizerThread = new Thread(new ThreadStart(() =>
                {
                    _dataVisualizationWindow = new DataVisualizationWindow();
                    _dataVisualizationWindow.Show();
                    _dataVisualizationWindow.Closed += VisualizerClosed;
                    try { Dispatcher.Run(); }
                    catch { return; }
                    return;
                }));
                VisualizerThread.Priority = ThreadPriority.AboveNormal;
                VisualizerThread.SetApartmentState(ApartmentState.STA);
                VisualizerThread.IsBackground = false;
                VisualizerThread.Start();
                while (true)
                {
                    Dispatcher dispatcher = Dispatcher.FromThread(VisualizerThread);
                    if (dispatcher == null)
                        Thread.Sleep(100);
                    else
                        break;
                }
            }
            else
            {
                Dispatcher dispatcher = Dispatcher.FromThread(VisualizerThread);
                if(!(dispatcher.HasShutdownStarted))
                    dispatcher.Invoke(() => _dataVisualizationWindow.Activate()) ;
            }
            state = VisualizerState.OPEN;
        }

        private void VisualizerClosed(object sender, EventArgs e) => Close();

        public void Hide()
        {
            if (state == VisualizerState.CLOSED || state == VisualizerState.HIDDEN) return;

            Dispatcher VisualizerDispatcher = Dispatcher.FromThread(VisualizerThread);
            if (VisualizerThread == null)
                throw new System.Exception("The visualizer dispatcher was not ready");
            VisualizerDispatcher.Invoke(() =>
            {
                _dataVisualizationWindow.Hide();
            });
            state = VisualizerState.HIDDEN;
        }

        public void Close()
        {
            state = VisualizerState.CLOSED;
            Dispatcher VisualizerDispatcher = Dispatcher.FromThread(VisualizerThread);
            if (VisualizerThread == null)
                throw new System.Exception("The visualizer dispatcher was not ready");
            VisualizerDispatcher.Invoke(() =>
            {
                (_dataVisualizationWindow.DataContext as DataVisualizationWindowViewModel).StopLogging();
                _dataVisualizationWindow.Close();
            });
            VisualizerDispatcher.InvokeShutdown();
            VisualizerThread = null;
        }
        #endregion
    }
}
