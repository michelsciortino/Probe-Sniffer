using Core.ViewModelBase;
using Ui.Models;
using Core;
using System.Collections.ObjectModel;
using System;

namespace Ui.ViewModels.DataVisualizer
{
    public class HiddenDevicesViewModel : BaseViewModel
    {

        #region Private Properties

        ObservableRangeCollection<HiddenDevice> _devices = null;
        private DateTime _startDate = DateTime.UtcNow, _endDate = DateTime.UtcNow.AddMinutes(1);
        private bool _isLoading = false, _hasData = false, _notFound = false;

        #endregion

        #region Constructor

        public HiddenDevicesViewModel()
        {
            _devices = new ObservableRangeCollection<HiddenDevice>();
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
    }
}
