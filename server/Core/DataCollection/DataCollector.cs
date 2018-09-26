using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Core.DBConnection;
using Core.DeviceCommunication;
using Core.DeviceCommunication.Messages.ESP32_Messages;
using Core.Models;

namespace Core.DataCollection
{
    
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
        private bool _runnig = false;
        #endregion

        #region Constructor

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
        public DataCollector()
        {
            dataCollectionTokenSource = new CancellationTokenSource();
            collectorThread = new Thread(() => DataCollectionLoop(dataCollectionTokenSource.Token));
            dbStoreThread = new Thread(() => DataStoreLoopAsync(dataCollectionTokenSource.Token));
            server = new TcpServer(ServerMode.DATACOLLECTION_MODE);
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

        public bool Initialize()
        {
            server.Start();
            if (server.IsStarted is false)
            {
                _initialized = false;
                return _initialized;
            }

            ESPManager.WaitForMinESPsToConnect();
            _initialized = true;
            return _initialized;
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
            while (token.IsCancellationRequested is false)
            {
                if (server.IsStarted)
                {
                    if (token.IsCancellationRequested) return;

                    server.NewMessageEvent.WaitOne(2000);
                    if (server.EnquedMessages == 0) continue;
                    
                    Data_Message message = server.GetNextMessage() as Data_Message;
                    if (message is null) continue;

                    DeviceData data = DeviceData.FromJson(message.Payload);
                    if (data is null) continue;

                    packetsMutex.WaitOne();
                    foreach (Packet p in data.Packets)
                    {
                        p.ESP_MAC = data.Esp_Mac;
                        packets.Enqueue(p);
                    }
                    packetsMutex.ReleaseMutex();
                }
            }
        }

        private async Task DataStoreLoopAsync(CancellationToken token)
        {
            Queue<Packet> toBeStored = new Queue<Packet>();
            bool any;
            while (true)
            {
                //Controllo la connessione con il database
                int tries = 0;
                while (tries < 3)
                {
                    if (dbConnection.Connected is true)
                        break;
                    else
                        dbConnection.Connect();
                    tries++;
                }
                if(dbConnection.Connected is false)
                { //Se la connessione è caduta, segnalo e riprovo dopo secondi
                    if (dbConnection.Connected is false)
                        ShowErrorMessage("Database connection has gone down.\nPackets will be stored locally waiting for a new connection.");
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
                        bool result= await dbConnection.StorePacketAsync(p);
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
        #endregion


        private void ShowErrorMessage(string message)
        {
            Core.Controls.MessageBox errorBox = new Core.Controls.MessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.None);
            errorBox.Show();
        }
    }
}
