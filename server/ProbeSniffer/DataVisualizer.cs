using Core.Models;
using ProbeSniffer.Windows;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace ProbeSniffer
{
    public class DataVisualizer
    {
        #region Private
        private bool _isShown = false;
        private Thread VisualizerThread = null;
        private DataVisualizationWindow _dataVisualizationWindow = null;
        private List<Device> _devices=null;
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
            if (_isShown) return;
            VisualizerThread = new Thread(new ThreadStart(() =>
            {
                _dataVisualizationWindow = new DataVisualizationWindow();
                _dataVisualizationWindow.Show();
                Dispatcher.Run();
            }));
            VisualizerThread.Priority = ThreadPriority.AboveNormal;
            VisualizerThread.SetApartmentState(ApartmentState.STA);
            VisualizerThread.IsBackground = false;
            VisualizerThread.Start();
            _isShown = true;
        }

        public void Hide()
        {
            if (!_isShown) return;
            Dispatcher VisualizerDispatcher = Dispatcher.FromThread(VisualizerThread);
            if (VisualizerThread == null)
                throw new System.Exception("The visualizer dispatcher was not ready");
            VisualizerDispatcher.Invoke(() =>
            {
                _dataVisualizationWindow.Close();
            });
            _isShown = false;
        }
        #endregion
    }
}
