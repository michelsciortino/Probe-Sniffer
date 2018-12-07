using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Core.DBConnection;
using Core.DeviceCommunication;
using Core.DeviceCommunication.Messages.ESP32_Messages;
using Core.Models;
using Core.Models.Database;

namespace Core.DataCollection
{

    public class DataCollector
    {
        #region Private Members
        private TcpServer server = null;
        private DatabaseConnection dbConnection = null;
        private Thread collectorThread = null;
        private Thread interplationThread = null;
        private CancellationTokenSource dataCollectionTokenSource = null;
        private Queue<Packet> toBeStored = null;
        private Queue<Packet> storeFails = null;
        #endregion

        #region Private Properties
        private bool _initialized = false;
        private bool _runnig = false;
        #endregion

        #region Constructor
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
        public DataCollector()
        {
            dataCollectionTokenSource = new CancellationTokenSource();
            collectorThread = new Thread(() => DataCollectionLoop(dataCollectionTokenSource.Token));
            interplationThread = new Thread(() => InterpolationLoopAsync(dataCollectionTokenSource.Token));
            server = new TcpServer(ServerMode.DATACOLLECTION_MODE);
            toBeStored = new Queue<Packet>();
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

        public bool Initialize()
        {
            server.Start();
            if (server.IsStarted is false)
            {
                _initialized = false;
                return _initialized;
            }

            //ESPManager.WaitForMinESPsToConnect();
            _initialized = true;
            return _initialized;
        }

        public void StartDataCollection()
        {
            if (_initialized is false) return;
            dbConnection.TestConnection();
            while (true)
            {
                if (dbConnection.Connected is false)
                { //Se la connessione è caduta, segnalo e riprovo dopo 10 secondi
                    if (dbConnection.Connected is false)
                        ShowErrorMessage("Database connection has gone down.\nPackets will be stored locally waiting for a new connection.");
                    Thread.Sleep(10000);
                    continue;
                }
                break;
            }
            server.CanReceiveData = true;
            collectorThread.Start();
            interplationThread.Start();
        }

        public void StopDataCollection()
        {
            server.CanReceiveData = false;
            try { dataCollectionTokenSource.Cancel(); }
            catch { }
        }

        private async Task DataCollectionLoop(CancellationToken token)
        {
            if (server.IsStarted is false) return;
            while (token.IsCancellationRequested is false)
            {
                dbConnection.TestConnection();
                if (dbConnection.Connected is false)
                { //Se la connessione è caduta, segnalo e riprovo dopo 10 secondi
                    if (dbConnection.Connected is false)
                        ShowErrorMessage("Database connection has gone down.\nPackets will be stored locally waiting for a new connection.");
                    Thread.Sleep(10000);
                    continue;
                }

                if (storeFails.Count == 0)
                { // get new messages only if there are no pendents packets
                    server.NewMessageEvent.WaitOne(2000);
                    while (server.EnquedMessages > 0)
                    { //ottengo tutti i nuovi messaggi dalla coda del server tcp
                        Data_Message message = server.GetNextMessage() as Data_Message;
                        if (message is null) continue;
                        DeviceData data = DeviceData.FromJson(message.Payload);
                        if (data is null)
                        {
                            Logger.Log("An ESP sent a wrong DEVICE_DATA message" + "\r\n");
                            continue;
                        }
                        var esp = ESPManager.GetESPDevice(data.Esp_Mac);
                        Logger.Log("An ESP sent DEVICE_DATA\t\tx: " + esp?.X_Position + " y: " + esp?.Y_Position + "\r\n");
                        foreach (Packet p in data.Packets)
                        {
                            p.ESP_MAC = data.Esp_Mac;
                            p.Timestamp = p.Timestamp.AddHours(2);
                            p.Hash = Utilities.Hash.SHA1(p.ESP_MAC + p.SSID + p.Seq_Num + p.Timestamp);
                            toBeStored.Enqueue(p);
                        }
                    }
                }
                else    //se ci sono dati non inviati al database, li reinserisco in coda			
                    while (storeFails.Count > 0)
                        toBeStored.Enqueue(storeFails.Dequeue());

                //Invio i dati al database
                while (toBeStored.Count > 0)
                {
                    Packet p = toBeStored.Dequeue();
                    try
                    {
                        bool result = await dbConnection.StorePacketAsync(p);
                        if (result is false)
                        {
                            storeFails.Enqueue(p);
                            break;
                        }
                    }
                    catch
                    { //Se lo store del dato è fallito, segnalo e interrompo
                        storeFails.Enqueue(p);
                        break;
                    }
                }

                //Se ci sono dati non inviati al database, li inserisco nella lista di fail
                while (toBeStored.Count > 0)
                    storeFails.Enqueue(toBeStored.Dequeue());
            }
        }

        /// <summary>
        /// This worker ask to the database for data to be processed
        /// Only data created before the 5 minutes of each loop are taken into account, the newers can be calculated in the next loop.
        /// If no data are avaible, the worker sleeps for one minute.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task InterpolationLoopAsync(CancellationToken token)
        {
            //Ottengo dal database l'ultimo intervallo salvato
            ProbesInterval last_interval = null;
            try
            {
                last_interval = await dbConnection.GetLastInterval();
            }
            catch
            {
                ShowErrorMessage("Database connection error.\nData Collection has been stopped");
                return;
            }
            if (last_interval is null)
            {
                while (true)
                {
                    last_interval = new ProbesInterval
                    {
                        Timestamp = await dbConnection.GetFirstTimestamp(),
                        IntervalId = -1
                    };
                    if (last_interval.Timestamp == default)
                    {
                        Thread.Sleep(30000);
                        continue;
                    }
                    break;
                }
                last_interval.Timestamp = last_interval.Timestamp.AddSeconds(-last_interval.Timestamp.Second).AddMinutes(-1);
            }

            while (token.IsCancellationRequested is false)
            {
                if (DateTime.Compare(DateTime.Now, last_interval.Timestamp.AddMinutes(5)) < 0)
                {
                    Thread.Sleep(30000);
                    continue;
                }

                ProbesInterval new_interval = new ProbesInterval
                {
                    IntervalId = last_interval.IntervalId + 1,
                    Timestamp = last_interval.Timestamp.AddMinutes(1),
                    Probes = new List<Probe>(),
                    ActiveEsps = new List<ESP32_Device>()
                };


                List<Packet> toBeProcessed = await dbConnection.GetRawData(new_interval.Timestamp, new_interval.Timestamp.AddMinutes(1));

                //Per ogni pacchetto nell'intervallo ottengo un dizionario di <Hash pacchetto, Lista pacchetti con lo stesso hash> per avere tutte le rilevazioni
                Dictionary<string, List<Packet>> DetectionForHash = new Dictionary<string, List<Packet>>();
                foreach (Packet p in toBeProcessed)
                {
                    if (DetectionForHash.ContainsKey(p.Hash))
                        DetectionForHash[p.Hash].Add(p);
                    else
                        DetectionForHash.Add(p.Hash, new List<Packet> { p });
                }

                //elimino dati che non servono più
                toBeProcessed.Clear();
                toBeProcessed = null;

                //Elimino tutti i probe non detected da tutti gli ESP
                //var toberemoved = DetectionForHash.Where(pair => pair.Value.Count < 2).Select(pair => pair.Key).ToList();
                var toberemoved = DetectionForHash.Where(pair => pair.Value.Count != ESPManager.ESPs.Count).Select(pair => pair.Key).ToList();
                if (toberemoved.Count!=0)
                {
                    foreach (string hash in toberemoved)
                        DetectionForHash.Remove(hash);
                    toberemoved = null;
                }
                HashSet<string> esps = new HashSet<string>();
                foreach (var detection in DetectionForHash)
                {
                    Point point = default;
                    List<KeyValuePair<ESP32_Device, int>> detections = new List<KeyValuePair<ESP32_Device, int>>();
                    foreach (Packet p in detection.Value)
                        detections.Add(new KeyValuePair<ESP32_Device, int>(ESPManager.GetESPDevice(p.ESP_MAC), p.SignalStrength));

                    try
                    {
                        point = Interpolator2.Interpolate(detections);
                    }
                    catch
                    {
                        continue;
                    }

                    if (point != null)
                    {
                        if (point == default) continue;
                        foreach (Packet p in detection.Value)
                            esps.Add(p.ESP_MAC);
                        new_interval.Probes.Add(new Probe()
                        {
                            SSID = detection.Value[0].SSID,
                            Sender = new Device()
                            {
                                MAC = detection.Value[0].MAC,
                                X_Position = point.X,
                                Y_Position = point.Y
                            },
                            Timestamp = detection.Value[0].Timestamp,
                            Hash = detection.Key
                        });
                    }
                }
                new_interval.ActiveEsps.AddRange(esps.Select((mac) => ESPManager.GetESPDevice(mac)).ToList());

                //Invio i dati al database
                if (new_interval.ActiveEsps.Count != 0)
                    try
                    {
                        bool result = await dbConnection.StoreProbesIntervalAsync(new_interval);
                        if (result is false)
                        {
                            dataCollectionTokenSource.Cancel();
                            ShowErrorMessage("Database connection error.\nData Collection has been stopped");
                            return;
                        }
                    }
                    catch
                    { //Se lo store del dato è fallito, segnalo e interrompo
                        dataCollectionTokenSource.Cancel();
                        ShowErrorMessage("Database connection error.\nData Collection has been stopped");
                        return;
                    }
                last_interval = new_interval;
            }
        }
        #endregion


        private void ShowErrorMessage(string message)
        {
            Core.Controls.MessageBox errorBox = new Core.Controls.MessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.None);
            errorBox.Show();
        }
    }
}
