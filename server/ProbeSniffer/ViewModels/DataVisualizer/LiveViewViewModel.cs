using Core.DBConnection;
using Core.Models;
using Core.ViewModelBase;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;

namespace ProbeSniffer.ViewModels.DataVisualizer
{
    public class LiveViewViewModel : BaseViewModel
    {
        #region Private Members
        private const int UPDATING_RATE = 1000;
        private Timer timer = null;
        private DatabaseConnection dbConnection = null;
        #endregion

        #region Private Properties
        private double _mapWidth=0;
        private double _mapHeight;
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
                if (d.X_Position > _mapWidth) _mapWidth = d.X_Position;
                if (d.Y_Position > _mapHeight) _mapHeight = d.Y_Position;
            }
            foreach (Device d in ESPDevices)
            {
                if (d.X_Position > _mapWidth) _mapWidth = d.X_Position;
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
        public Double MapWidth => _mapWidth + 20; //added offset for device size
        public Double MapHeight => _mapHeight + 20;
        #endregion

        #region Public Methods

        public async Task UpdateAsync(object dispatcher)
        {
            if ((Dispatcher)dispatcher is null)
                return;
            /*IntervalDescriptionEntry lastIntervalDescription = await dbConnection.GetLastIntervalDescritpionEntryAsync();
            List<IntervalDataEntry> lastIntervalData = await dbConnection.GetLastIntervalDataEntriesAsync();

            Dictionary<string, Device> lastIntervalDetectedDevices = new Dictionary<string, Device>();
            foreach(IntervalDataEntry entry in lastIntervalData)
            {
                if (lastIntervalDetectedDevices.ContainsKey(entry.Sender.MAC))
                    lastIntervalDetectedDevices[entry.Sender.MAC] = entry.Sender;
                else
                    lastIntervalDetectedDevices.Add(entry.Sender.MAC, entry.Sender);
            }

            ((Dispatcher)dispatcher).Invoke(() =>
            {
                _devices.Clear();
                foreach(Device d in lastIntervalDetectedDevices.Values)
                    _devices.Add(d);
                _espDevices.Clear();
                foreach (Device d in lastIntervalDescription.ActiveEsps)
                    _espDevices.Add(d);
                if (_points.Count >= 20)
                    _points.RemoveAt(0);
                ObservableCollection<Point> newPoints = _points;
                newPoints.Add(new Point(lastIntervalDescription.IntervalId, lastIntervalDescription.NumberOfDetectedDevices));
                _points = newPoints;
            });*/

            #region Test
            ((Dispatcher)dispatcher).Invoke(() => {
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
            #endregion
        }

        public void RunUpdater() => timer.Change(0, UPDATING_RATE);
        public void StopUpdater() => timer.Change(Timeout.Infinite, UPDATING_RATE);
        #endregion
    }
}
