using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ProbeSniffer.Models
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
        /// Get the stored configuration
        /// </summary>
        /// <returns>A configuration if found, null otherwise</returns>
        public Configuration LoadConfiguration()
        {
            return Deserialize();
        }

        /// <summary>
        /// Store the configuration to disk
        /// </summary>
        /// <returns>True if saved correctly, False otherwise</returns>
        public bool SaveConfiguration()
        {
            return Serialize(this);
        }
        #endregion

        #region Serialization

        /// <summary>
        /// Serialize a Configuration to file
        /// </summary>
        /// <param name="configuration">Configuration to serialize</param>
        /// <returns>True if serialized correctly, False otherwise</returns>
        private static bool Serialize(Configuration configuration)
        {
            IFormatter formatter = new BinaryFormatter();
            try
            {
                Stream stream = new FileStream(ConfFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, configuration);
                stream.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deserializes a configuration from file
        /// </summary>
        /// <returns>A configuration if found and deserialized correctly, null otherwise</returns>
        private static Configuration Deserialize()
        {
            Configuration c = null;

            if (!File.Exists(ConfFilePath))
                return null;
            IFormatter formatter = new BinaryFormatter();
            try
            {
                Stream stream = new FileStream(ConfFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                c = (Configuration)formatter.Deserialize(stream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return c;
        }

        #endregion
    }
}
