using Core.DBConnection;
using Core.Models;
using Core.ViewModelBase;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using Core.DataCollection;
using System.Windows;
using Core.DeviceCommunication;

namespace Ui.ViewModels.DataVisualizer
{
    public class LiveViewViewModel : BaseViewModel
    {
        #region Private Members
        private const int UPDATING_RATE = 60000;
        private Timer timer = null;
        private DatabaseConnection dbConnection = null;
        #endregion

        #region Private Properties
        private double _mapWidth=0;
        private readonly double _mapHeight;
        #endregion

        #region Constructor
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
        public LiveViewViewModel()
        {
            timer = new Timer((dispatcher) => UpdateAsync(dispatcher),Dispatcher.CurrentDispatcher,Timeout.Infinite,UPDATING_RATE);
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();
            #region TESTING
            Devices = new ObservableCollection<Device>();
            ESPDevices = new ObservableCollection<ESP32_Device>
            {
                new ESP32_Device() { X_Position = 0, Y_Position = 0, MAC = "00:00:00:00:00:00" },
                new ESP32_Device() { X_Position = 400, Y_Position = 0, MAC = "00:00:00:00:00:00" },
                new ESP32_Device() { X_Position = 0, Y_Position = 400, MAC = "00:00:00:00:00:00" },
                new ESP32_Device() { X_Position = 400, Y_Position = 400, MAC = "00:00:00:00:00:00" }
            };

            foreach (Device d in Devices)
            {
                if (d.X_Position > MapWidth1) MapWidth1 = d.X_Position;
                if (d.Y_Position > _mapHeight) _mapHeight = d.Y_Position;
            }
            foreach (Device d in ESPDevices)
            {
                if (d.X_Position > MapWidth1) MapWidth1 = d.X_Position;
                if (d.Y_Position > _mapHeight) _mapHeight = d.Y_Position;
            }

            Points = new ObservableCollection<KeyValuePair<int,string>>();
            #endregion
        }
        #endregion

        #region Public Properties
        public ObservableCollection<Device> Devices { get; set; }
        public ObservableCollection<ESP32_Device> ESPDevices { get; set; }
        public ObservableCollection<KeyValuePair<int, string>> Points { get; set; }
        public Double MapWidth => MapWidth1 + 20; //added offset for device size
        public Double MapHeight => _mapHeight + 20;

        public double MapWidth1 { get => _mapWidth; set => _mapWidth = value; }
        #endregion

        #region Public Methods

        public async Task UpdateAsync(object dispatcher)
        {
            StopUpdater();
            Dispatcher disp = dispatcher as Dispatcher;
            if(disp is null)
                return;
            DateTime old = DateTime.Now;
            old=old.Subtract(new TimeSpan(0,2,0));
            DateTime newd = DateTime.Now;

            List<Packet> intervalPackets=await dbConnection.GetIntervalPacketAsync(old, newd);

            //Per ogni pacchetto nell'intervallo ottengo un dizionario di <Hash pacchetto, Lista pacchetti con lo stesso hash>
            Dictionary<string, List<Packet>> PacketForHash = new Dictionary<string, List<Packet>>();
            foreach(Packet p in intervalPackets)
            {
                if (PacketForHash.ContainsKey(p.Hash))
                    PacketForHash[p.Hash].Add(p);
                else
                    PacketForHash.Add(p.Hash, new List<Packet> { p });
            }

            //elimino dati che non servono più
            intervalPackets.Clear();

            //Elimino tutti i probe non detected da almeno 2 ESP
            Dictionary<string, List<Packet>> PurgedPacketForHash = new Dictionary<string, List<Packet>>();
            foreach(var pair in PacketForHash)
            {
                if (pair.Value.Count >= 2)
                    PurgedPacketForHash.Add(pair.Key,pair.Value);
            }
            PacketForHash = PurgedPacketForHash;

            //Per ogni mac di device trovato, prendo l'ultimo probe che questo ha generato nell'intervallo
            Dictionary<string, string> latestHashForMac = new Dictionary<string, string>();

            foreach (string key in PacketForHash?.Keys)
            {
                List<Packet> value = PacketForHash[key];
                if (latestHashForMac.ContainsKey(value[0].MAC))
                {
                    if (PacketForHash[latestHashForMac[value[0].MAC]][0].Timestamp > value[0].Timestamp)
                        latestHashForMac[value[0].MAC] = key;
                }
                else
                    latestHashForMac.Add(value[0].MAC, key);
            }

            List<Device> newMapDevices = new List<Device>();

            Dictionary<string,ESP32_Device> esps = ESPManager.ESPs.ToDictionary(e => e.MAC);
            foreach (var pair in esps) pair.Value.Active = false;

            foreach(KeyValuePair<string,string> pair in latestHashForMac)
            {
                Point point= default;
                List<KeyValuePair<ESP32_Device, int>> detections = new List<KeyValuePair<ESP32_Device, int>>();
                List<Packet> packets = PacketForHash[pair.Value];

                foreach(Packet p in packets)
                {
                    detections.Add(new KeyValuePair<ESP32_Device, int>(esps[p.ESP_MAC], p.SignalStrength));
                    if (esps[p.ESP_MAC].Active == false) esps[p.ESP_MAC].Active = true;
                }


                try
                {
                    point = Interpolator.Interpolate(detections);
                }
                catch
                {
                    continue;
                }

                newMapDevices.Add(new Device
                {
                    MAC = pair.Key,
                    Timestamp = PacketForHash[pair.Value][0].Timestamp,
                    X_Position = point.X,
                    Y_Position = point.Y
                });
            }

            latestHashForMac.Clear();
            PacketForHash.Clear();

            disp.Invoke(() =>
            {
                Devices.Clear();
                foreach (Device d in newMapDevices)
                    Devices.Add(d);
                    ESPDevices.Clear();
                foreach (ESP32_Device d in esps.Values)
                    if(d.Active)
                    ESPDevices.Add(d);
                if (Points.Count >= 20)
                    Points.RemoveAt(0);
                Points.Add(new KeyValuePair<int, string>(Devices.Count, newd.Hour.ToString()+":"+newd.Minute.ToString()));
            });

            #region Test
            /*
            disp.Invoke(() => {
                int Id;
                if(Points.Count>=20)
                    Points.RemoveAt(0);
                if(Devices.Count > 50)
                    Devices.RemoveAt(0);
                Random r = new Random(DateTime.Now.Millisecond);
                int newp = (int)(r.NextDouble() * 1000); if (newp < 0) newp = (-newp);
                int newx = (int)(r.NextDouble() * 1000); if (newx < 0) newx = (-newx);
                int newy = (int)(r.NextDouble() * 1000); if (newy < 0) newy = (-newy);
                if (Points.Count == 0) Id = 0;
                else Id = int.Parse(Points[Points.Count - 1].Value) + 1;
                //Points.Add(new KeyValuePair<int, string>(0,(Id).ToString()));
                Points.Add(new KeyValuePair<int, string>(newp % 250,(Id).ToString()));
                Devices.Add(new Device { MAC = "AA:BB:CC:DD:EE:FF", X_Position= newx % 400 , Y_Position= newy%400  });
            });
            */
            #endregion
            RunUpdater();
        }

        public void RunUpdater() => timer.Change(UPDATING_RATE, UPDATING_RATE);
        public void StopUpdater() => timer.Change(Timeout.Infinite, Timeout.Infinite);
        #endregion
    }
}
