using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using Core.DBConnection;
using Core.DeviceCommunication;
using Core.Models;

namespace Core.DataCollection
{
    public class DataCollector
    {
        #region Private Properties
        private DeviceConnectionManager _deviceConnectionManager;
        private bool _initialized = false;
        private Thread _dataCollectionThread = null;
        private bool _runnig;
        private CancellationTokenSource _tokenSource = null;
        private int _collectionIntervalRate = 60;
        #endregion

        #region Constructor
        public DataCollector(DeviceConnectionManager connectionManager)
        {
            _deviceConnectionManager = connectionManager;
            _tokenSource = new CancellationTokenSource();
            _dataCollectionThread = new Thread(() => DataCollectionMainLoop(_tokenSource.Token,_deviceConnectionManager));
        }
        #endregion

        #region Public Propeties
        public bool Initialized => _initialized;
        public bool Running => _runnig;
        #endregion

        #region Private Methods
        private void DataCollectionMainLoop(CancellationToken token,DeviceConnectionManager connectionManager)
        {
            Thread processingThread = null;
            List<DeviceData> intervalData;
            bool result;
            while (true)
            {
                //Sending Timestamp to each device
                result = connectionManager.SendTimestampsToDevices();
                if (result is false)
                {
                    string fault_message = "Some devices died (?)\n";
                    foreach (ESP32_Device d in connectionManager.Devices)
                    {
                        fault_message += "\t" + d.MAC + " X:" + d.X_Position + " Y:" + d.Y_Position + "\n";
                    }
                    MessageBox.Show(fault_message,
                                    "Communication Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error,
                                    MessageBoxResult.None,
                                    MessageBoxOptions.ServiceNotification);
                }
                
                //Waiting for the ESP devices to collect 
                Thread.Sleep(_collectionIntervalRate);

                intervalData = connectionManager.CollectDevicesData();

                if (processingThread != null)
                    try
                    {
                        processingThread.Join();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    
                processingThread = new Thread(() => ProcessDeviceData(intervalData,connectionManager.Devices));
                processingThread.Start();
            }
        }

        private void ProcessDeviceData(List<DeviceData> intervalData,List<ESP32_Device> eSP32_Devices)
        {
            DatabaseConnection dbConnection=new DatabaseConnection();
            List<DatabaseEntry> newEntries = new List<DatabaseEntry>();

            int tries = 0;
            while (tries < 3)
            {
                dbConnection.Connect();
                if (dbConnection.Connected)
                    break;
                Thread.Sleep(2000);
                tries++;
            }
            if (dbConnection.Connected)
                _initialized = true;

            int IntervalId = dbConnection.GetMaxIntervalID().Result+1;

            
            //Filtering the Packets lists
            intervalData=FilterDeviceDataList(intervalData);

            List<Device> ActiveESPs = eSP32_Devices.FindAll(esp => intervalData.FindAll(d => d.Esp_Mac == esp.MAC).Any()).
                                                    Select(esp => new Device
                                                    {
                                                        MAC = esp.MAC,
                                                        X_Position = esp.X_Position,
                                                        Y_Position = esp.Y_Position
                                                    }).ToList();

            //Creating the database entries calculating the devices position
            foreach (Packet packet in intervalData[0].Packets)
            {
                DatabaseEntry entry = new DatabaseEntry
                {
                    Hash = packet.Hash,
                    IntervalId = IntervalId,
                    SSID = packet.SSID,
                    Timestamp = packet.Timestamp,
                    ActiveESP32s = ActiveESPs,
                    Device = new Device()
                };
                entry.Device.MAC = packet.MAC;
                //TO DO
                entry.Device.X_Position = -1;//???
                entry.Device.Y_Position = -1;//??
                
                newEntries.Add(entry);
            }

            dbConnection.Store(newEntries);
        }

        private List<DeviceData> FilterDeviceDataList( List<DeviceData> dirty)
        {
            //ordering each packet list on packet.Hash
            foreach(DeviceData data in dirty)
                data.Packets.OrderBy(packet => packet.Hash);

            //foreach packet list
            for(int i=0;i< dirty.Count;i++)
            {
                //removing all the packet not present in each of the other packet lists
                for (int j = 0; j < dirty.Count; i++)
                {
                    //if the same list of the external loop => continue
                    if (j == i) continue;

                    dirty[i].Packets.RemoveAll(packet => !dirty[j].Packets.FindAll(p => p.Hash == packet.Hash).Any());
                }
            }

            return dirty;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the Collection process
        /// </summary>
        /// <returns>True if initialized, False otherwise</returns>
        public bool Initialize()
        {
            if (_initialized)
                return _initialized;

            _initialized = _deviceConnectionManager.Initialized;

            return _initialized;
        }

        /// <summary>
        /// Runs the DataCollection Process
        /// </summary>
        /// <param name="collectionIntervalRate">The collection interval duration in seconds</param>
        public void Run(int collectionIntervalRate)
        {
            if (_runnig) return;
            _collectionIntervalRate = collectionIntervalRate;
            _runnig = true;
            _dataCollectionThread.Start();
        }
        #endregion
    }
}
