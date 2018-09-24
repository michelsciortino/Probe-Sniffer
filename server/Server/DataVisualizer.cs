using Core.Models;
using System;
using System.Collections.Generic;

namespace Server
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
        private VisualizerState state = VisualizerState.CLOSED;
        #endregion

        #region Constructor

        public DataVisualizer(IList<Device> devices)
        {
        }
        #endregion

        #region Public Methods
        public void Show()
        {
            if (state == VisualizerState.CLOSED)
            {
                
            }
            else
            {
                
            }
            state = VisualizerState.OPEN;
        }

        private void VisualizerClosed(object sender, EventArgs e) => Close();

        public void Hide()
        {
            if (state == VisualizerState.CLOSED || state == VisualizerState.HIDDEN) return;

            
            state = VisualizerState.HIDDEN;
        }

        public void Close()
        {
            state = VisualizerState.CLOSED;
           
        }
        #endregion
    }
}
