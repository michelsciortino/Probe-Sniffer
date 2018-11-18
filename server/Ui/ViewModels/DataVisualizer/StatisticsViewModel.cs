using Core.ViewModelBase;
using Ui.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Core.DBConnection;
using System.Windows;
using System;
using Core.Models.Database;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Windows.Threading;

namespace Ui.ViewModels.DataVisualizer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
    public class StatisticsViewModel : BaseViewModel
    {
        #region Private Members
        private DatabaseConnection dbConnection = null;
        #endregion

        #region Private Properties
        private ObservableCollection<DeviceStatistic> _devices = null;
        private DateTime _startDate= DateTime.UtcNow, _endDate= DateTime.UtcNow.AddMinutes(1);
        private Precision _precision;
        private bool _isLoading = false, _hasData = false, _notFound = false;
        #endregion

        #region Constructor
        public StatisticsViewModel()
        {
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();
            _devices = new ObservableCollection<DeviceStatistic>();
            Values = new ObservableRangeCollection<KeyValuePair<int[], SolidColorBrush>>();
            Labels = new ObservableRangeCollection<string>();
            _loadStatisticsCommand = new RelayCommand<object>((x) => LoadStatistics());
            _uncheckAllCommand = new RelayCommand<object>((x) => UncheckAll());
        }
        #endregion
        
        #region Public Properties
        public ObservableRangeCollection<KeyValuePair<int[], SolidColorBrush>> Values { get; set; }
        public ObservableRangeCollection<string> Labels { get; set; }

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

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate == value) return;
                if (DateTime.Compare(value, _endDate) >= 0)
                    _startDate = _endDate.AddMinutes(-1);
                else
                    _startDate = value;
                OnPropertyChanged(nameof(StartDate));
            }
        }
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate == value) return;
                if (DateTime.Compare(value, _startDate) <0)
                    _endDate = _startDate.AddMinutes(1);
                else
                    _endDate = value;
                OnPropertyChanged(nameof(EndDate));
            }
        }
        public Precision Precision
        {
            get => _precision;
            set
            {
                _precision = value;
                OnPropertyChanged(nameof(Precision));
            }
        }

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

        public bool HasData
        {
            get => _hasData;
            set
            {
                if (value == _hasData) return;
                _hasData = value;
                OnPropertyChanged(nameof(HasData));
            }
        }

        public bool NotFound
        {
            get => _notFound;
            set
            {
                if (value == _notFound) return;
                _notFound = value;
                OnPropertyChanged(nameof(NotFound));
            }
        }
        #endregion
        
        #region Private Commands
        private ICommand _loadStatisticsCommand = null;
        private ICommand _uncheckAllCommand = null;
        #endregion

        #region Public Commands
        public ICommand LoadStatisticsCommand => _loadStatisticsCommand;
        public ICommand UncheckAllCommand => _uncheckAllCommand;
        #endregion

        #region Private Methods

        private void UncheckAll()
        {
            foreach (var d in Devices)
            {
                if (d.Active == false) continue;
                d.PropertyChanged -= checkedChange;
                d.Active = false;
                d.PropertyChanged += checkedChange;
            }
            Values.Clear();
        }
        


        public async Task LoadStatistics()
        {
            var disp = Dispatcher.CurrentDispatcher;
            NotFound = false;
            HasData = false;
            if (dbConnection == null || dbConnection.TestConnection() == false)
            {
                Core.Controls.MessageBox message = new Core.Controls.MessageBox("Unable to connect to the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                message.Show();
                return;
            }
            IsLoading = true;

            DateTime start = _startDate;
            DateTime end = _endDate;
            Precision precision = _precision;

            List<ProbesInterval> intervals = await dbConnection.GetIntervalsBetween(start, end);

            if (intervals is null)
            {
                IsLoading = false;
                Core.Controls.MessageBox message = new Core.Controls.MessageBox("No data found.", "", MessageBoxButton.OK, MessageBoxImage.Information);
                message.Show();
                return;
            }
            List<DeviceStatistic> statistics = DeviceStatistic.DoStatistics(intervals, start, end, precision);
            if (statistics.Count == 0)
            {
                IsLoading = false;
                HasData = false;
                NotFound = true;
                return;
            }
            statistics.Sort((a, b) => a.Tot_Probes > b.Tot_Probes ? 1 : 0);
            statistics = statistics.OrderByDescending((a) => a.Tot_Probes).ToList();
            foreach(var d in Devices) d.PropertyChanged -= checkedChange;
            Devices.Clear();

            foreach (var st in statistics)
            {
                st.PropertyChanged += checkedChange;
                Devices.Add(st);
            }
            Labels.Clear();
            Values.Clear();
            List<string> new_lables = new List<string>();
            if (precision == Models.Precision.Day)
            {
                DateTime label = start.AddHours(-start.Hour).AddMinutes(-start.Minute).AddSeconds(-start.Second).AddMinutes(-start.Millisecond);
                for (int i = (int)(end - start).TotalDays; i > 0; i--)
                {
                    new_lables.Add(label.ToShortDateString());
                    label = label.AddDays(1);
                }
            }
            else if (precision == Models.Precision.Hour)
            {
                DateTime label = start.AddMinutes(-start.Minute).AddSeconds(-start.Second).AddMinutes(-start.Millisecond);
                for (int i = (int)(end - start).TotalHours; i > 0; i--)
                {
                    new_lables.Add(label.ToShortTimeString() + "\n" + label.ToShortDateString());
                    label = label.AddHours(1);
                }
            }

            Labels.AddRange(new_lables);
            Values.AddRange(_devices.Where((d) => d.Active is true).Select((d) => new KeyValuePair<int[], SolidColorBrush>(d.Probes, d.Color)));
            HasData = true;
            IsLoading = false;
            NotFound = false;
        }

        private void checkedChange(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Values.Clear();
            Values.AddRange(_devices.Where((d) => d.Active is true).Select((d) => new KeyValuePair<int[], SolidColorBrush>(d.Probes, d.Color)));
        }
    }

    #endregion
}