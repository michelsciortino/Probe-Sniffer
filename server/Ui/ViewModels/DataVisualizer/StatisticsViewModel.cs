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
        private ObservableCollection<DeviceStatistics> _devices = null;
        private DateTime _startDate, _endDate;
        private Precision _precision;
        private bool _isLoading = false, _hasData = false, _notFound = false;
        #endregion

        #region Constructor
        public StatisticsViewModel()
        {
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();
            _devices = new ObservableCollection<DeviceStatistics>();
            Values = new ObservableRangeCollection<KeyValuePair<int[], SolidColorBrush>>();
            Labels = new ObservableRangeCollection<string>();
            _loadStatisticsCommand = new RelayCommand<object>((x) => LoadStatistics());
        }
        #endregion


        #region Public Properties
        public ObservableRangeCollection<KeyValuePair<int[], SolidColorBrush>> Values { get; set; }
        public ObservableRangeCollection<string> Labels { get; set; }

        public ObservableCollection<DeviceStatistics> Devices
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
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
            }
        }
        public string Precision
        {
            get => _precision.ToString();
            set
            {
                if (value == "Hour")
                    _precision = Models.Precision.HOUR;

                if (value == "Day")
                    _precision = Models.Precision.DAY;
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
        #endregion

        #region Public Commands
        public ICommand LoadStatisticsCommand => _loadStatisticsCommand;
        #endregion
        #region Private Methods
        public async Task LoadStatistics()
        {
            var disp = Dispatcher.CurrentDispatcher;
            NotFound = false;
            HasData = false;
            if (dbConnection == null || dbConnection.TestConnection() == false)
            {
                MessageBox.Show("Unable to connect to the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("No data found.", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            List<DeviceStatistics> statistics = DeviceStatistics.DoStatistics(intervals, start, end, precision);
            if (statistics.Count == 0)
            {
                IsLoading = false;
                HasData = false;
                NotFound = true;
                return;
            }
            statistics.Sort((a, b) => a.Tot_Probes > b.Tot_Probes ? 1 : 0);
            statistics = statistics.OrderByDescending((a) => a.Tot_Probes).ToList();
            Devices.Clear();
            foreach (var st in statistics)
                Devices.Add(st);
            Labels.Clear();
            Values.Clear();
            List<string> new_lables = new List<string>();
            if (precision == Models.Precision.DAY)
            {
                DateTime label = start.AddHours(-start.Hour).AddMinutes(-start.Minute).AddSeconds(-start.Second).AddMinutes(-start.Millisecond);
                for (int i = (int)(end - start).TotalDays; i > 0; i--)
                {
                    new_lables.Add(label.ToShortDateString());
                    label = label.AddDays(1);
                }
            }
            else if (precision == Models.Precision.HOUR)
            {
                DateTime label = start.AddMinutes(-start.Minute).AddSeconds(-start.Second).AddMinutes(-start.Millisecond);
                for (int i = (int)(end - start).TotalHours; i > 0; i--)
                {
                    new_lables.Add(label.ToShortTimeString() + "\n" + label.ToShortDateString());
                    label = label.AddHours(1);
                }
            }

            Labels.AddRange(new_lables);
            Values.AddRange(_devices.Where((d) => d.Active is true).Select((d) => new KeyValuePair<int[], SolidColorBrush>(d.Probes, d.LineColor)));
            HasData = true;
            IsLoading = false;
            NotFound = false;
        }
    }

    #endregion


    #region Private Methods

    #endregion
}