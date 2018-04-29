using System;
using System.Collections.Generic;

namespace Core.Models
{
    [Serializable]
    public class Configuration
    {
        const string ConfFilePath = ".conf";
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

        /// <summary>
        /// Getter for the list of devices
        /// </summary>
        public IList<Device> Devices => _devices;

        /// <summary>
        /// Get the stored configuration
        /// </summary>
        /// <returns>A configuration if found, null otherwise</returns>
        public Configuration LoadConfiguration()
        {
            return Serializer.Deserialize<Configuration>(ConfFilePath);
        }

        /// <summary>
        /// Store the configuration to disk
        /// </summary>
        /// <returns>True if saved correctly, False otherwise</returns>
        public bool SaveConfiguration()
        {
            return Serializer.Serialize(this, ConfFilePath);
        }
        #endregion

    }
}
