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
        public LiveViewViewModel()
        {
            timer = new Timer((dispatcher) => UpdateAsync(dispatcher),Dispatcher.CurrentDispatcher,Timeout.Infinite,UPDATING_RATE);
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();


            #region TESTING
            Devices = new ObservableCollection<Device>();
            Devices.Add(new Device() { X_Position = 50, Y_Position = 80, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 17, Y_Position = 180, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 215, Y_Position = 320, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 10, Y_Position = 130, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 400, Y_Position = 280, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 27, Y_Position = 80, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 164, Y_Position = 54, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 215, Y_Position = 127, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 1, Y_Position = 94, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 300, Y_Position = 280, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 167, Y_Position = 80, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 258, Y_Position = 61, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 198, Y_Position = 357, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 168, Y_Position = 247, MAC = "00:00:00:00:00:00" });
            Devices.Add(new Device() { X_Position = 158, Y_Position = 34, MAC = "00:00:00:00:00:00" });
            
            ESPDevices = new ObservableCollection<Device>();
            ESPDevices.Add(new Device() { X_Position = 0, Y_Position = 0, MAC = "00:00:00:00:00:00" });
            ESPDevices.Add(new Device() { X_Position = 400, Y_Position = 0, MAC = "00:00:00:00:00:00" });
            ESPDevices.Add(new Device() { X_Position = 0, Y_Position = 400, MAC = "00:00:00:00:00:00" });
            ESPDevices.Add(new Device() { X_Position = 400, Y_Position = 400, MAC = "00:00:00:00:00:00" });

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
            Points.Add(new KeyValuePair<int, string>(100,"1"));
            Points.Add(new KeyValuePair<int, string>(180,"2"));
            Points.Add(new KeyValuePair<int, string>(170,"3"));
            Points.Add(new KeyValuePair<int, string>(100,"4"));
            Points.Add(new KeyValuePair<int, string>(140,"5"));
            Points.Add(new KeyValuePair<int, string>(140,"6"));
            Points.Add(new KeyValuePair<int, string>(187,"7"));
            Points.Add(new KeyValuePair<int, string>(192,"8"));
            Points.Add(new KeyValuePair<int, string>(176,"9"));
            Points.Add(new KeyValuePair<int, string>(140,"10"));
            Points.Add(new KeyValuePair<int, string>(110,"11"));
            Points.Add(new KeyValuePair<int, string>(100,"12"));
            Points.Add(new KeyValuePair<int, string>(100,"13"));
            Points.Add(new KeyValuePair<int, string>(120,"14"));
            Points.Add(new KeyValuePair<int, string>(150,"15"));
            Points.Add(new KeyValuePair<int, string>(136,"16"));
            Points.Add(new KeyValuePair<int, string>(186,"17"));
            Points.Add(new KeyValuePair<int, string>(194,"18"));
            Points.Add(new KeyValuePair<int, string>(136,"19"));
            Points.Add(new KeyValuePair<int, string>(136,"20"));
            #endregion
        }
        #endregion

        #region Public Properties
        public ObservableCollection<Device> Devices { get; set; }
        public ObservableCollection<Device> ESPDevices { get; set; }
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
                Points.RemoveAt(0);
                Random r = new Random(DateTime.Now.Second);
                int newv = (int)(r.NextDouble() * 1000 % 250); if (newv < 0) newv = (-newv) % 250;
                Points.Add(new KeyValuePair<int, string>(newv,(int.Parse(Points[Points.Count-1].Value)+1).ToString()));
            });
            #endregion
        }

        public void RunUpdater() => timer.Change(0, UPDATING_RATE);
        public void StopUpdater() => timer.Change(Timeout.Infinite, UPDATING_RATE);
        #endregion
    }
}
