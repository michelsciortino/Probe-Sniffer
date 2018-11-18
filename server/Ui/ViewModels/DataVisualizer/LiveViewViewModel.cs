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
using System.Windows;
using Core.Models.Database;
using Ui.Models;

namespace Ui.ViewModels.DataVisualizer
{
    public class LiveViewViewModel : BaseViewModel
    {
        #region Private Members
        private const int UPDATING_RATE = 30000;
        private Timer timer = null;
        private DatabaseConnection dbConnection = null;
        private DateTime last_interval_timestamp= default;
        #endregion

        #region Private Properties
        private double _mapWidth=20;
        private double _mapHeight=20;
        private bool _isLoading = true;
        #endregion

        #region Constructor
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
        public LiveViewViewModel()
        {
            last_interval_timestamp = DateTime.Now.AddMinutes(-3);
            timer = new Timer((dispatcher) => UpdateAsync(dispatcher),Dispatcher.CurrentDispatcher,Timeout.Infinite,UPDATING_RATE);
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();
            dbConnection.TestConnection();
            Devices = new ObservableRangeCollection<DeviceStatistic>();
            ESPDevices = new ObservableCollection<ESP32_Device>();
            Points = new ObservableCollection<KeyValuePair<int,string>>();
        }

        #endregion

        #region Public Properties
        public ObservableRangeCollection<DeviceStatistic> Devices { get; set; }
        public ObservableCollection<ESP32_Device> ESPDevices { get; set; }
        public ObservableCollection<KeyValuePair<int, string>> Points { get; set; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (value == _isLoading) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public Double MapHeight
        {
            get => _mapHeight;
            set
            {
                _mapHeight = value + 20;
                OnPropertyChanged(nameof(MapHeight));
            }
        }
        public Double MapWidth
        {
            get => _mapWidth;
            set
            {
                _mapWidth = value + 20;//offset
                OnPropertyChanged(nameof(MapWidth));
            }
        }
        #endregion

        #region Private Methods
        private List<Probe> GetLastPositions(ProbesInterval interval)
        {
            Dictionary<string, Probe> last = new Dictionary<string, Probe>();            
            foreach(var p in interval.Probes)
            {
                if (!last.ContainsKey(p.Sender.MAC)) last.Add(p.Sender.MAC, p);
                else
                {
                    if (last[p.Sender.MAC].Timestamp < p.Timestamp) last[p.Sender.MAC] = p;
                }
            }
            return last.Values.ToList();
        }
        #endregion

        #region Public Methods

        public async Task UpdateAsync(object dispatcher)
        {
            StopUpdater();
            Dispatcher disp = dispatcher as Dispatcher;
            if(disp is null)
                return;
            if (dbConnection.TestConnection() == false)
            {
                Core.Controls.MessageBox message = new Core.Controls.MessageBox("Unable to connect to the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                message.Show();
                return;
            }

            List<ProbesInterval> stored_intervals = await dbConnection.GetIntervalsBetween(last_interval_timestamp.AddMinutes(-20), last_interval_timestamp);
            stored_intervals.Sort((a, b) => DateTime.Compare(a.Timestamp, b.Timestamp));
            ProbesInterval[] new_intervals = new ProbesInterval[20];
            KeyValuePair<int, string>[] new_points = new KeyValuePair<int, string>[20];
            foreach (var interval in stored_intervals)
                new_intervals[(interval.Timestamp - last_interval_timestamp.AddMinutes(-20)).Minutes] = interval;
            List<Probe> i_last = null;

            
            for (int i = 0; i < 20; i++)
            {
                var ts = last_interval_timestamp.AddMinutes(-20 + i);
                new_points[i] = new KeyValuePair<int, string>(0, ts.Hour.ToString() + ":" +
                                                            (ts.Minute < 10 ? "0" : "") + ts.Minute.ToString() + "\n" +
                                                             ts.Date.ToShortDateString());
            }
            

            for(int i=0;i<20;i++)
            {
                i_last = null;
                if (new_intervals[i] == null) continue;
                i_last = GetLastPositions(new_intervals[i]);                
                new_points[i]=new KeyValuePair<int, string>(i_last.Count, new_intervals[i].Timestamp.Hour.ToString() + ":"+
                                                            (new_intervals[i].Timestamp.Minute<10?"0":"") + new_intervals[i].Timestamp.Minute.ToString()+"\n"+
                                                             new_intervals[i].Timestamp.Date.ToShortDateString());
            }

            disp.Invoke(() =>
            {
                Points.Clear();
                foreach (var p in new_points)
                {
                    Points.Add(p);
                }
                Devices.Clear();
                ESPDevices.Clear();
                if (i_last != null)
                {
                    Devices.AddRange(i_last.Select((p) =>
                    {
                        return new DeviceStatistic
                        {
                            MAC = p.Sender.MAC,
                            SSID = p.SSID,
                            X_Position = p.Sender.X_Position,
                            Y_Position = p.Sender.Y_Position,
                            Color = null
                        };
                    }).ToList());
                    foreach (ESP32_Device esp in new_intervals.Last()?.ActiveEsps)
                    {
                        if (MapWidth < esp.X_Position) MapWidth = esp.X_Position;
                        if (MapHeight < esp.Y_Position) MapHeight = esp.Y_Position;
                        ESPDevices.Add(esp);
                    }
                }
            });
            if ((DateTime.Now.AddMinutes(-3) - last_interval_timestamp).Minutes >= 1)
                last_interval_timestamp = last_interval_timestamp.AddMinutes(1);
            RunUpdater();
            IsLoading = false;
        }

        public void RunUpdater() => timer.Change(UPDATING_RATE/3, UPDATING_RATE);
        public void StopUpdater() => timer.Change(Timeout.Infinite, Timeout.Infinite);
        #endregion
    }
}
