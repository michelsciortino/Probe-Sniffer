using Core.Models;
using Core.Models.Database;
using Core.ViewModelBase;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Ui.Models;
using Core.DBConnection;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Ui.ViewModels.DataVisualizer
{
    public class SliderViewViewModel : BaseViewModel
    {
        #region Members
        private DatabaseConnection dBConnection;
        private DeviceStatistic[][] devices;
        private ESP32_Device[][] esps;
        private Dictionary<string, DeviceStatistic> found_devices;
        private DateTime start_date=new DateTime(), end_date=new DateTime();
        #endregion

        #region Constructor
        public SliderViewViewModel()
        {
            _isLoading = false;
            _hasData = false;
            _notFound = false;
            _date = DateTime.UtcNow;
            _start = DateTime.UtcNow;
            _end = DateTime.UtcNow.AddMinutes(1);
            _devices = new ObservableCollection<DeviceStatistic>();
            ShownDevices = new ObservableRangeCollection<DeviceStatistic>();
            ESPDevices = new ObservableRangeCollection<ESP32_Device>();
            dBConnection = new DatabaseConnection();
            dBConnection.Connect();
            _loadDataCommand = new RelayCommand<object>((x) => LoadDataAsync());
            _checkAllCommand = new RelayCommand<object>((x) => CheckAll());
            _uncheckAllCommand = new RelayCommand<object>((x) => UncheckAll());
            _slideCommand = new RelayCommand<object>((x) => Update(null,null));
        }
        #endregion

        #region Private Properties
        private ObservableCollection<DeviceStatistic> _devices = null;
        private ObservableCollection<ESP32_Device> _esps=null;

        private DateTime _date, _start, _end;
        private Precision _precision;
        private bool _isLoading, _hasData, _notFound, _sliderVisibility;
        private int _intervalsN, _selectedIntervalN;
        #endregion

        #region Public Properties
        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged(nameof(Date));
            }
        }
        public DateTime Start
        {
            get => _start;
            set
            {
                _start = value;
                OnPropertyChanged(nameof(Start));
            }
        }
        public DateTime End
        {
            get => _end;
            set
            {
                _end = value;
                OnPropertyChanged(nameof(End));
                OnPropertyChanged(nameof(End2));
            }
        }

        public DateTime End2 => End.AddMinutes(-1);

        public Precision Precision
        {
            get => _precision;
            set
            {
                _precision = value;
                OnPropertyChanged(nameof(Precision));
            }
        }
        public int IntervalsN
        {
            get => _intervalsN;
            set
            {
                _intervalsN = value-1;
                OnPropertyChanged(nameof(IntervalsN));
                if (value > 1) SliderVisibility = true;
                else SliderVisibility = false;
            }
        }
        public int SelectedIntervalN
        {
            get => _selectedIntervalN;
            set
            {
                _selectedIntervalN = value;
                OnPropertyChanged(nameof(SelectedIntervalN));
                OnPropertyChanged(nameof(SelectedIntervalTime));
                Update(null, null);
            }
        }
        public string SelectedIntervalTime
        {
            get =>start_date.AddMinutes(_selectedIntervalN).ToShortTimeString();
        }
        #region Private Commands
        private ICommand _loadDataCommand;
        private ICommand _slideCommand;
        private ICommand _uncheckAllCommand;
        private ICommand _checkAllCommand;
        #endregion

        #region Public Commands
        public ICommand LoadDataCommand => _loadDataCommand;
        public ICommand SlideCommand => _slideCommand;
        public ICommand UncheckAllCommand => _uncheckAllCommand;
        public ICommand CheckAllCommand => _checkAllCommand;
        #endregion
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }
        public bool NotFound
        {
            get => _notFound;
            set
            {
                _notFound = value;
                OnPropertyChanged(nameof(NotFound));
            }
        }
        public bool HasData
        {
            get => _hasData;
            set
            {
                _hasData = value;
                OnPropertyChanged(nameof(HasData));
            }
        }
        public bool SliderVisibility
        {
            get => _sliderVisibility;
            set
            {
                _sliderVisibility = value;
                OnPropertyChanged(nameof(SliderVisibility));
            }
        }
        public ObservableRangeCollection<DeviceStatistic> ShownDevices { get; set; }

        public ObservableCollection<DeviceStatistic> Devices
        {
            get => _devices;
            set
            {
                if (_devices == value) return;
                _devices = value;
                OnPropertyChanged(nameof(Devices));
            }
        }
        public ObservableRangeCollection<ESP32_Device> ESPDevices { get; set; }
        #endregion


        #region Private Methods
        private void CheckAll()
        {
            foreach (var d in Devices)
            {
                if (d.Active == true) continue;
                d.PropertyChanged -= Update;
                d.Active = true;
                d.PropertyChanged += Update;
            }
            ShownDevices.Clear();
            ShownDevices.AddRange(devices[_selectedIntervalN].Where((p) => found_devices[p.MAC].Active is true).ToList());
        }
        private void UncheckAll()
        {
            foreach (var d in Devices)
            {
                if (d.Active == false) continue;
                d.PropertyChanged -= Update;
                d.Active = false;
                d.PropertyChanged += Update;
            }
            ShownDevices.Clear();
        }
        private void Update(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ESPDevices.Clear();
            ESPDevices.AddRange(esps[_selectedIntervalN]);
            ShownDevices.Clear();
            ShownDevices.AddRange(devices[_selectedIntervalN].Where((p) => found_devices[p.MAC].Active is true).ToList());
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            HasData = false;
            NotFound = false;

            devices = null;
            esps = null;
            found_devices = null;
            ShownDevices.Clear();
            foreach (var d in Devices) d.PropertyChanged -= Update;
            Devices.Clear();
            ESPDevices.Clear();
            start_date = _date;
            start_date = start_date.AddHours(-start_date.Hour).AddMinutes(-start_date.Minute).AddSeconds(-start_date.Second).AddMilliseconds(-start_date.Millisecond);
            end_date = start_date;
            start_date = start_date.AddHours(_start.Hour).AddMinutes(_start.Minute);
            end_date = end_date.AddHours(_end.Hour).AddMinutes(_end.Minute);

            List<ProbesInterval> intervals = await dBConnection.GetIntervalsBetween(start_date, end_date);
            if (intervals.Count == 0)
            {
                NotFound = true;
                HasData = false;
                IsLoading = false;
                return;
            }

            devices = new DeviceStatistic[(int)(end_date - start_date).TotalMinutes][];
            esps = new ESP32_Device[(int)(end_date - start_date).TotalMinutes][];
            found_devices = new Dictionary<string, DeviceStatistic>();
            foreach (var i in intervals)
            {
                int index = (int)(i.Timestamp - start_date).TotalMinutes;
                esps[index] = new ESP32_Device[i.ActiveEsps.Count];
                for (int e = 0; e < i.ActiveEsps.Count; e++) esps[index][e] = i.ActiveEsps[e];
                Dictionary<string, List<Probe>> dict = new Dictionary<string, List<Probe>>();

                foreach (var p in i.Probes)
                {
                    if (!dict.ContainsKey(p.Sender.MAC))
                        dict.Add(p.Sender.MAC, new List<Probe>());
                    dict[p.Sender.MAC].Add(p);
                }

                devices[index] = new DeviceStatistic[dict.Count];
                var enumerator = dict.GetEnumerator();
                for (int d = 0; d < dict.Count; d++)
                {
                    enumerator.MoveNext();

                    if (!found_devices.ContainsKey(enumerator.Current.Key))
                        found_devices.Add(enumerator.Current.Key,
                            new DeviceStatistic
                            {
                                Active = true,
                                MAC = enumerator.Current.Key,
                                Color = Styles.Colors.Next,
                                Probes = null,
                                SSID = "",
                                Tot_Probes = enumerator.Current.Value.Count
                            });
                    else
                        found_devices[enumerator.Current.Key].Tot_Probes += enumerator.Current.Value.Count;

                    DeviceStatistic ds = new DeviceStatistic
                    {
                        SSID = "",
                        MAC = enumerator.Current.Key,
                        X_Position = 0,
                        Y_Position = 0,
                        Color= found_devices[enumerator.Current.Key].Color
                    };
                    foreach (var dpd in enumerator.Current.Value)
                    {
                        ds.X_Position += dpd.Sender.X_Position;
                        ds.Y_Position += dpd.Sender.Y_Position;
                    }
                    ds.X_Position /= enumerator.Current.Value.Count;
                    ds.Y_Position /= enumerator.Current.Value.Count;
                    devices[index][d] = ds;
                }
                dict.Clear();
            }

            IntervalsN = devices.Length;
            found_devices.OrderBy((d) => d.Key);
            foreach (var d in found_devices)
            {
                d.Value.PropertyChanged += Update;
                _devices.Add(d.Value);
            }
            OnPropertyChanged(nameof(Devices));
            SelectedIntervalN = 0;

            if (found_devices.Count != 0)
            {
                ShownDevices.AddRange(devices[0].Where((d) => found_devices[d.MAC].Active is true).ToList());
                ESPDevices.AddRange(esps[0]);
                HasData = true;
                NotFound = false;
                IsLoading = false;
            }
            else
            {
                HasData = false;
                NotFound = true;
                IsLoading = false;
            }
        }        
        #endregion
    }
}
