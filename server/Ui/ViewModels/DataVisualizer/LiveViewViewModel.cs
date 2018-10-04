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
using Core.Models.Database;

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
        #endregion

        #region Constructor
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
        public LiveViewViewModel()
        {
            timer = new Timer((dispatcher) => UpdateAsync(dispatcher),Dispatcher.CurrentDispatcher,Timeout.Infinite,UPDATING_RATE);
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();
            dbConnection.TestConnection();
            Probes = new ObservableCollection<Probe>();
            ESPDevices = new ObservableCollection<ESP32_Device>();
            Points = new ObservableCollection<KeyValuePair<int,string>>();
        }

        #endregion

        #region Public Properties
        public ObservableCollection<Probe> Probes { get; set; }
        public ObservableCollection<ESP32_Device> ESPDevices { get; set; }
        public ObservableCollection<KeyValuePair<int, string>> Points { get; set; }
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
                MessageBox.Show("Unable to connect to the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<ProbesInterval> new_intervals = await dbConnection.GetLastIntervalsAfter(last_interval_timestamp);
            if (new_intervals.Count == 0)
            {
                RunUpdater();
                return;
            }

            //Per ogni mac di device trovato, prendo l'ultimo probe che questo ha generato nell'intervallo
            new_intervals.Sort((a, b) => DateTime.Compare(a.Timestamp,b.Timestamp));

            List<Probe> i_last = null;
            var new_points = new List<KeyValuePair<int, string>>();            
            foreach (var interval in new_intervals)
            {
                i_last = GetLastPositions(interval);
                last_interval_timestamp = interval.Timestamp;
                new_points.Add(new KeyValuePair<int, string>(i_last.Count, interval.Timestamp.Hour.ToString() + ":"+
                                                            (interval.Timestamp.Minute<10?"0":"") + interval.Timestamp.Minute.ToString()+"\n"+
                                                             interval.Timestamp.Date.ToShortDateString()));
            }
            disp.Invoke(() =>
            {
                foreach (var p in new_points)
                {
                    if (Points.Count >= 20)
                        Points.RemoveAt(0);
                    Points.Add(p);
                }
                Probes.Clear();
                foreach (Probe p in i_last)
                    Probes.Add(p);
                ESPDevices.Clear();
                foreach (ESP32_Device esp in new_intervals[new_intervals.Count - 1].ActiveEsps)
                {
                    if (MapWidth < esp.X_Position) MapWidth = esp.X_Position;
                    if (MapHeight < esp.Y_Position) MapHeight = esp.Y_Position;
                    ESPDevices.Add(esp);
                }
            });
            RunUpdater();
        }

        public void RunUpdater() => timer.Change(UPDATING_RATE/3, UPDATING_RATE);
        public void StopUpdater() => timer.Change(Timeout.Infinite, Timeout.Infinite);
        #endregion
    }
}
