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

namespace Ui.ViewModels.DataVisualizer
{
    public class StatisticsViewModel : BaseViewModel
    {
        #region Private Members
        private DatabaseConnection dbConnection=null;
        #endregion

        #region Private Properties
        private ObservableCollection<DeviceStatistics> _devices = null;
        private DateTime _startDate,_endDate;
        #endregion

        #region Constructor
        public StatisticsViewModel()
        {
            dbConnection = new DatabaseConnection();
            dbConnection.Connect();
            _devices = new ObservableCollection<DeviceStatistics>();
        }
        #endregion


        #region Public Properties
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
        #endregion


        #region Private Commands
        private ICommand _search = null;
        #endregion

        #region Public Commands
        public void /* async Task*/ Search()
        {
            if(dbConnection==null || dbConnection.TestConnection() == false)
            {
                MessageBox.Show("Unable to connect to the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime start = _startDate;
            DateTime end = _endDate;

        }
        #endregion


        #region Private Methods
        private void DoStatistics(List<ProbesInterval> intervals,DateTime start,DateTime end)
        {
            int hours = end.Subtract(start).Hours;

        }
        #endregion
    }
}
