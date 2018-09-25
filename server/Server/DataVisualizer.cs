using System;

namespace Server
{
    public class DataVisualizer
    {
        private enum VisualizerState
        {
            CLOSED = 0,
            OPEN,
        }

        private ChildProcessHost UiProcess;
        
        #region Private
        private VisualizerState state = VisualizerState.CLOSED;
        #endregion

        #region Constructor
        public DataVisualizer()
        {
            UiProcess = new ChildProcessHost("ProbeSnifferUi");
            UiProcess.Child_exited += UiProcess_Child_exited;
        }

        private void UiProcess_Child_exited(object sender)
        {
            if (UiProcess.Running is false) state = VisualizerState.CLOSED;
        }
        #endregion

        #region Public Methods
        public void Show()
        {
            if (state == VisualizerState.CLOSED)
            {
                if(UiProcess.Running==false)
                    UiProcess.Start("ui_pipe");
                state = VisualizerState.OPEN;
            }
        }

        private void VisualizerClosed(object sender, EventArgs e) => Close();

        public void Close()
        {
            state = VisualizerState.CLOSED;
            UiProcess.Dispose();
        }
        #endregion
    }
}
