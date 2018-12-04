using Core.ViewModelBase;
using Ui.Models;
using Core;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Core.Models.Database;
using Core.DBConnection;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using Core.Models;

namespace Ui.ViewModels.DataVisualizer
{
    public class HiddenDevicesViewModel : BaseViewModel
    {
        #region Private Members
        private DatabaseConnection dbConnection = null;
        #endregion

        #region Private Properties

        ObservableRangeCollection<HiddenDevice> _devices = null;
        private DateTime _startDate = DateTime.UtcNow, _endDate = DateTime.UtcNow.AddMinutes(1);
        private bool _isLoading = false, _hasData = false, _notFound = false;

        #endregion

        #region Constructor

        public HiddenDevicesViewModel()
        {
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();
            dbConnection.TestConnection();
            _devices = new ObservableRangeCollection<HiddenDevice>();
            _loadDeviceListCommand = new RelayCommand<object>(x => LoadDeviceListAsync());
        }

        #endregion

        #region Public Properties

        public ObservableRangeCollection<HiddenDevice> Devices
        {
            get => _devices;
            set{
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
                if (DateTime.Compare(value, _startDate) < 0)
                    _endDate = _startDate.AddMinutes(1);
                else
                    _endDate = value;
                OnPropertyChanged(nameof(EndDate));
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
        private ICommand _loadDeviceListCommand = null;
        #endregion

        #region Public Commands
        public ICommand LoadDeviceListCommand => _loadDeviceListCommand;
        #endregion

        #region Private Methods
        private async System.Threading.Tasks.Task LoadDeviceListAsync()
        {
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

            List<ProbesInterval> intervals = await dbConnection.GetIntervalsBetween(start, end);

            if (intervals is null)
            {
                IsLoading = false;
                Core.Controls.MessageBox message = new Core.Controls.MessageBox("No data found.", "", MessageBoxButton.OK, MessageBoxImage.Information);
                message.Show();
                NotFound = true;
                return;
            }

            if (intervals.Count == 0)
            {
                NotFound = true;
                HasData = false;
                IsLoading = false;
                return;
            }

            List<Probe> probes = new List<Probe>();
            foreach (var interval in intervals)
                probes.AddRange(interval.Probes);

            List<HiddenDeviceInfo> hiddenDeviceInfos = Core.Utilities.HiddenDevicesFinder.Find(probes);
            Devices.Clear();
            Devices.AddRange(hiddenDeviceInfos.Select(hdi => new HiddenDevice { Id = hdi.Id, MacList = hdi.MacList.ToList() }));
            HasData = true;
            NotFound = false;
            IsLoading = false;

        }
        #endregion
    }
}
