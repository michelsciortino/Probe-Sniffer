using Core.Models;
using Core.ViewModelBase;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;

namespace Configurator.ViewModels
{
    public class ConfiguratorViewModel : BaseViewModel
    {
        #region Private Variables
        private Configuration configuration = null;
        private Thread ServerAdvertismentThread = null;
        #endregion

        #region Constructor
        public ConfiguratorViewModel()
        {
            configuration = Configuration.LoadConfiguration();
            if (configuration is null) configuration = new Configuration();
            _devices = new ObservableCollection<Device>(configuration.Devices);
        }
        #endregion

        #region Private properties
        private ObservableCollection<Device> _devices = null;
        private bool _newDeviceFormEnabled = true, _addDeviceButtonEnabled=false;
        private double _x,_y;
        private string _mac="";
        #endregion

        #region Public Properties
        public ObservableCollection<Device> Devices
        {
            get =>_devices;
            set
            {
                if (_devices == value) return;
                _devices = value;
                OnPropertyChanged(nameof(Devices));
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

        public double X
        {
            get => _x;
            set
            {
                if (_x == value) return;
                _x = value;
                OnPropertyChanged(nameof(X));
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                if (_y == value) return;
                _y = value;
                OnPropertyChanged(nameof(Y));
            }
        }

        public string MAC
        {
            get => _mac;
            set
            {
                if (_mac == value) return;
                _mac = value;
                OnPropertyChanged(nameof(MAC));
                if (value == "") AddDeviceButtonEnabled = false;
                else AddDeviceButtonEnabled = true;
            }
        }
        
        public bool AddDeviceButtonEnabled
        {
            get => _addDeviceButtonEnabled;
            set { if (_addDeviceButtonEnabled == value) return; _addDeviceButtonEnabled = value; OnPropertyChanged(nameof(AddDeviceButtonEnabled)); }
        }
        #endregion

        #region Private Commands
        private ICommand _addDeviceCommand = null;
        private ICommand _removeDeviceCommand = null;
        private ICommand _saveConfigurationCommand = null;
        #endregion

        #region Public Commands
        public ICommand AddDeviceCommand => _addDeviceCommand ?? (_addDeviceCommand = new RelayCommand<object>((x) => AddDevice()));
        public ICommand RemoveDeviceCommand => _removeDeviceCommand ?? (_removeDeviceCommand = new RelayCommand<Device>((x) => RemoveDevice(x)));
        public ICommand SaveConfigurationCommand => _saveConfigurationCommand ?? (_saveConfigurationCommand = new RelayCommand<object>( (x) => SaveConfiguration()));
        
        #endregion

        #region Private Methods
        private void AddDevice()
        {
            if (_mac == "") return;
            Devices.Add(new Device { X_position = _x, Y_Position = _y, MAC = _mac, Active = false });
        }

        private void RemoveDevice(Device x)
        {
            Devices.Remove(x);
        }

        private void SaveConfiguration()
        {
            configuration = new Configuration();
            foreach (Device d in Devices)
                configuration.AddDevice(d);
            configuration.SaveConfiguration();
        }
        #endregion
    }
}
