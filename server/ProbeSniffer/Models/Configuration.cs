using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeSniffer.Models
{
    [Serializable]
    public class Configuration
    {
        #region Private

        private int _nDevices = 0;
        private List<Device> _devices = null;

        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public Configuration()
        {
            this._devices = new List<Device>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a device to the configuration
        /// </summary>
        /// <param name="device">A device to add</param>
        public void AddDevice(Device device)
        {
            _devices.Add(device);
            _nDevices++;
        }

        /// <summary>
        /// Getter for the number of devices in the configuration
        /// </summary>
        /// <returns>The number of devices</returns>
        public int GetNumOfDevices() => _nDevices;
        #endregion
    }
}
