using Core.Models;
using Core.ViewModelBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Configurator.ViewModels
{
    public class ConfiguratorViewModel : BaseViewModel
    {
        #region Private Variables
        private Configuration configuration = null;
        #endregion

        #region Constructor
        public ConfiguratorViewModel()
        {
            configuration = Configuration.LoadConfiguration();
            if (configuration is null) configuration = new Configuration();
            _devices = new ObservableCollection<Device>(configuration.Devices);
            _selectedSSID = configuration.SSID;
        }
        #endregion

        #region Private properties
        private ObservableCollection<Device> _devices = null;
        private bool _newDeviceFormEnabled = true, _addDeviceButtonEnabled = false, _saveConfigurationButtonEnabled = false;
        private string _x = null, _y = null, _mac = null, _selectedSSID = null;
        private ObservableCollection<string> _ssidsList = null;
        #endregion

        #region Public Properties
        public ObservableCollection<Device> Devices
        {
            get => _devices;
            set
            {
                if (_devices == value) return;
                _devices = value;
                OnPropertyChanged(nameof(Devices));
            }
        }

        public ObservableCollection<string> SSIDList
        {
            get => _ssidsList;
            set
            {
                if (_ssidsList == value) return;
                _ssidsList = value;
                OnPropertyChanged(nameof(SSIDList));
            }
        }

        public bool NewDeviceFormEnabled
        {
            get => _newDeviceFormEnabled;
            set
            {
                if (_newDeviceFormEnabled == value) return;
                _newDeviceFormEnabled = value;
                OnPropertyChanged(nameof(NewDeviceFormEnabled));
            }
        }

        public string X
        {
            get => _x;
            set
            {
                _x = value;
                CheckNewDeviceParameters();
                OnPropertyChanged(nameof(X));
            }
        }

        public string Y
        {
            get => _y;
            set
            {
                _y = value;
                CheckNewDeviceParameters();
                OnPropertyChanged(nameof(Y));
            }
        }

        public string MAC
        {
            get => _mac;
            set
            {
                
                _mac = value;
                CheckNewDeviceParameters();
                OnPropertyChanged(nameof(MAC));
            }
        }

        public string SelectedSSID
        {
            get => _selectedSSID;
            set
            {
                if (value is null)
                {
                    SaveConfigurationButtonEnabled = false;
                    return;
                }
                if (_selectedSSID == value)
                    return;
                _selectedSSID = value;
                SaveConfigurationButtonEnabled = true;
                OnPropertyChanged(nameof(SelectedSSID));
            }
        }


        public bool AddDeviceButtonEnabled
        {
            get => _addDeviceButtonEnabled;
            set { if (_addDeviceButtonEnabled == value) return; _addDeviceButtonEnabled = value; OnPropertyChanged(nameof(AddDeviceButtonEnabled)); }
        }

        public bool SaveConfigurationButtonEnabled
        {
            get => _saveConfigurationButtonEnabled;
            set { if (_saveConfigurationButtonEnabled == value) return; _saveConfigurationButtonEnabled = value; OnPropertyChanged(nameof(SaveConfigurationButtonEnabled)); }
        }
        #endregion

        #region Private Commands
        private ICommand _addDeviceCommand = null;
        private ICommand _removeDeviceCommand = null;
        private ICommand _saveConfigurationCommand = null;
        private ICommand _updateAvaibleSSIDsListCommand = null;
        #endregion

        #region Public Commands
        public ICommand AddDeviceCommand => _addDeviceCommand ?? (_addDeviceCommand = new RelayCommand<object>((x) => AddDevice()));
        public ICommand RemoveDeviceCommand => _removeDeviceCommand ?? (_removeDeviceCommand = new RelayCommand<Device>((x) => RemoveDevice(x)));
        public ICommand SaveConfigurationCommand => _saveConfigurationCommand ?? (_saveConfigurationCommand = new RelayCommand<object>((x) => SaveConfiguration()));
        public ICommand UpdateAvaibleSSIDsListCommand => _updateAvaibleSSIDsListCommand ?? (_updateAvaibleSSIDsListCommand = new RelayCommand<object>((x) => UpdateAvaibleSSIDsList()));
        #endregion

        #region Private Methods
        private void AddDevice()
        {
            if (_mac == "") return;
            Devices.Add(new Device { X_Position = Double.Parse(_x), Y_Position = Double.Parse(_y), MAC = _mac });
            MAC = "";
        }

        private void RemoveDevice(Device x)
        {
            Devices.Remove(x);
        }

        private void UpdateAvaibleSSIDsList()
        {
            SSIDList = new ObservableCollection<string>();
            List<string> ssids = LocalNetworkConnection.GetAvaibleWifiNetworksSSIDList().Distinct().OrderBy(s => s).ToList();
            if (ssids is null) return;
            foreach (string ssid in ssids)
                _ssidsList.Add(ssid);
        }

        private void CheckNewDeviceParameters()
        {
            Double value;

            if (_mac is null || _mac == "")
            {
                AddDeviceButtonEnabled = false;
                return;
            }

            try
            {
                value = Double.Parse(_x);
                value = Double.Parse(_y);
            }
            catch
            {
                AddDeviceButtonEnabled = false;
                return;
            }
            AddDeviceButtonEnabled = true;
        }

        private void SaveConfiguration()
        {
            configuration = new Configuration();
            foreach (Device d in Devices)
                configuration.AddDevice(d);
            configuration.SSID = _selectedSSID;
            configuration.SaveConfiguration();
        }
        #endregion
    }
}
