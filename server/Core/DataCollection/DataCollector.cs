using System.Collections.Generic;
using System.Threading;
using System.Windows;
using Core.DBConnection;
using Core.DeviceCommunication;
using Core.DeviceCommunication.Messages.ESP32_Messages;
using Core.Models;

namespace Core.DataCollection
{
    #region Old DataCollector
    /*
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
            _dataCollectionThread = new Thread(() => DataCollectionMainLoop(_tokenSource.Token, _deviceConnectionManager));
        }
        #endregion

        #region Public Propeties
        public bool Initialized => _initialized;
        public bool Running => _runnig;
        #endregion

        #region Private Methods
        private void DataCollectionMainLoop(CancellationToken token, DeviceConnectionManager connectionManager)
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

                processingThread = new Thread(() => ProcessDevicesDataAsync(intervalData, connectionManager.Devices));
                processingThread.Start();
            }
        }

        private async Task ProcessDevicesDataAsync(List<DeviceData> intervalData, List<ESP32_Device> eSP32_Devices)
        {
            DatabaseConnection dbConnection = new DatabaseConnection();
            IntervalDescriptionEntry newIntervalDescription = null;
            List<IntervalDataEntry> newIntervalData = null;
            int newIntervalId;

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


            //Getting the last IntervalId
            IntervalDescriptionEntry lastIntervalDescription = await dbConnection.GetLastIntervalDescritpionEntryAsync();

            if (lastIntervalDescription is null || dbConnection.Connected)
                newIntervalId = 0;
            else
                newIntervalId = lastIntervalDescription.IntervalId + 1;

            //Filtering the Packets lists
            intervalData = FilterDeviceDataList(intervalData);

            Dictionary<string, ESP32_Device> ActiveESPs = eSP32_Devices.FindAll(esp => intervalData.FindAll(d => d.Esp_Mac == esp.MAC).Any()).ToDictionary(d => d.MAC);



            newIntervalData = new List<IntervalDataEntry>();


            for (int i = 0; i < intervalData[0].Packets.Count; i++)
            {

                List<KeyValuePair<ESP32_Device, int>> packetInterpolationData = new List<KeyValuePair<ESP32_Device, int>>();
                foreach (DeviceData dd in intervalData)
                    packetInterpolationData.Add(new KeyValuePair<ESP32_Device, int>(ActiveESPs[dd.Esp_Mac], dd.Packets[i].SignalStrength));

                KeyValuePair<double, double> senderPosition = new KeyValuePair<double, double>();//TODO=Interpolator.FindPacketSenderPosition(packetInterpolationData);
                IntervalDataEntry entry = new IntervalDataEntry
                {
                    Hash = intervalData[0].Packets[i].Hash,
                    IntervalId = lastIntervalDescription.IntervalId,
                    SSID = intervalData[0].Packets[i].SSID,
                    Timestamp = intervalData[0].Packets[i].Timestamp,
                    Sender = new Device { MAC = intervalData[0].Packets[i].MAC, X_Position = senderPosition.Key, Y_Position = senderPosition.Value }
                };

                newIntervalData.Add(entry);
            }



            newIntervalDescription = new IntervalDescriptionEntry
            {
                ActiveEsps = ActiveESPs.Values.ToList().ConvertAll(esp => new Device { MAC = esp.MAC, X_Position = esp.X_Position, Y_Position = esp.Y_Position }).ToList(),
                IntervalId = newIntervalId,
                Timestamp = DateTime.Now,
            };


            await dbConnection.StoreIntervalDescriptionEntryAsync(newIntervalDescription);
            await dbConnection.StoreIntervalDataEntriesAsync(newIntervalData);
        }

        private List<DeviceData> FilterDeviceDataList(List<DeviceData> dirty)
        {
            //ordering each packet list on packet.Hash
            foreach (DeviceData data in dirty)
                data.Packets.OrderBy(packet => packet.Hash);

            //foreach packet list
            for (int i = 0; i < dirty.Count; i++)
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
    }*/
    #endregion

    #region New DataCollector
    public class DataCollector
    {
        #region Private Members
        private TcpServer server = null;
        private DatabaseConnection dbConnection = null;
        private Thread collectorThread = null;
        private Thread dbStoreThread = null;
        private CancellationTokenSource dataCollectionTokenSource = null;
        private Queue<Packet> packets = null;
        private Queue<Packet> storeFails = null;
        private Mutex packetsMutex = null;
        #endregion


        #region Private Properties
        private bool _initialized = false;        
        private bool _runnig;
        #endregion

        #region Constructor
        public DataCollector()
        {
            dataCollectionTokenSource = new CancellationTokenSource();
            collectorThread = new Thread(() => DataCollectionLoop(dataCollectionTokenSource.Token));
            dbStoreThread = new Thread(() => DataStoreLoop(dataCollectionTokenSource.Token));
            server = new TcpServer();
            packets = new Queue<Packet>();
            packetsMutex = new Mutex();
            storeFails = new Queue<Packet>();
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();
        }
        #endregion

        #region Public Propeties
        public bool Initialized => _initialized;
        public bool Running => _runnig;
        #endregion

        #region Private Methods

        public void Initialize()
        {
            server.Start();
            if (server.IsStarted)
            {
                _initialized = false;
                return;
            }

            ESPManager.WaitForMinESPsToConnect();
            _initialized = true;
        }

        public void StartDataCollection()
        {
            if (_initialized is false) return;
            server.CanReceiveData = true;
            collectorThread.Start();
            dbStoreThread.Start();
        }

        public void StopDataCollection()
        {
            server.CanReceiveData = false;
            try { dataCollectionTokenSource.Cancel(); }
            catch { }                
        }

        private void DataCollectionLoop(CancellationToken token)
        {
            Queue<Data_Message> messages = null;
            while (token.IsCancellationRequested is false)
            {
                if (server.IsStarted)
                {
                    while (server.EnquedMessages == 0)
                    {
                        if (token.IsCancellationRequested) return;
                        Thread.Sleep(100);
                    }
                    messages = server.GetNewMessages();

                    packetsMutex.WaitOne();
                    while (messages.Count > 0)
                    {
                        Data_Message message = messages.Dequeue();

                        DeviceData data = DeviceData.FromJson(message.Payload);
                        if (data is null) continue;
                        foreach (Packet p in data.Packets)
                        {
                            p.ESP_MAC = data.Esp_Mac;
                            packets.Enqueue(p);
                        }
                    }
                    packetsMutex.ReleaseMutex();
                }
            }
        }

        private void DataStoreLoop(CancellationToken token)
        {
            Queue<Packet> toBeStored = new Queue<Packet>();
            bool any;
            while (true)
            {
                //Controllo la connessione con il database
                int tries = 0;
                while (tries < 3)
                {
                    if (dbConnection.Connected is true) break;
                    else dbConnection.Connect();
                }
                if(dbConnection.Connected is false)
                { //Se la connessione è caduta, segnalo e riprovo dopo secondi
                    if (dbConnection.Connected is false)
                        MessageBox.Show("Database connection has gone down.");
                    Thread.Sleep(5000);
                    continue;
                }

                //se ci sono dati non inviati al database, li reinserisco in coda
                while (storeFails.Count > 0)
                    toBeStored.Enqueue(storeFails.Dequeue());

                packetsMutex.WaitOne();
                any = packets.Count > 0;
                packetsMutex.ReleaseMutex();

                //Se non ci sono dati da salvare dormo 1 secondo
                if(any is false && toBeStored.Count is 0)
                {
                    //Se la cancellazione è richiesta ritorno
                    if (token.IsCancellationRequested is true)
                        return;
                    Thread.Sleep(1000);
                    continue;
                }

                if (any)
                {
                    //Ottengo i nuovi dati
                    packetsMutex.WaitOne();
                    while (packets.Count > 0)
                        toBeStored.Enqueue(packets.Dequeue());
                    packetsMutex.ReleaseMutex();
                }

                //Invio i dati al database
                while (toBeStored.Count > 0)
                {
                    Packet p = toBeStored.Dequeue();
                    try
                    {
                        //dbConnection.Store(p);
                    }
                    catch
                    { //Se lo store del dato è fallito, segnalo e interrompo
                        storeFails.Enqueue(p);
                        if (dbConnection.Connected is false)
                            MessageBox.Show("Database connection has gone down.");
                        break;
                    }
                }
                //Se ci sono dati non inviati al database, li inserisco nella lista di fail
                while (toBeStored.Count > 0)
                    storeFails.Enqueue(toBeStored.Dequeue());
            }
        }
        #endregion
    }
    #endregion

}
