using Core.Models;
using Core.ViewModelBase;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace ProbeSniffer.ViewModels.DataVisualizer
{
    public class LiveViewViewModel : BaseViewModel
    {
        #region Private Properties
        private ObservableCollection<Device> _devices = null;
        private ObservableCollection<Device> _espDevices = null;
        private ObservableCollection<Point> _points =null;
        private double _mapWidth=0;
        private double _mapHeight;
        #endregion

        #region Constructor
        public LiveViewViewModel()
        {
            #region TESTING
            _devices = new ObservableCollection<Device>();
            _devices.Add(new Device() { X_Position = 50, Y_Position = 80, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 17, Y_Position = 180, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 215, Y_Position = 320, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 10, Y_Position = 130, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 400, Y_Position = 280, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 27, Y_Position = 80, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 164, Y_Position = 54, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 215, Y_Position = 127, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 1, Y_Position = 94, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 300, Y_Position = 280, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 167, Y_Position = 80, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 258, Y_Position = 61, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 198, Y_Position = 357, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 168, Y_Position = 247, MAC = "00:00:00:00:00:00" });
            _devices.Add(new Device() { X_Position = 158, Y_Position = 34, MAC = "00:00:00:00:00:00" });

            _espDevices = new ObservableCollection<Device>();
            _espDevices.Add(new Device() { X_Position = 0, Y_Position = 0, MAC = "00:00:00:00:00:00" });
            _espDevices.Add(new Device() { X_Position = 400, Y_Position = 0, MAC = "00:00:00:00:00:00" });
            _espDevices.Add(new Device() { X_Position = 0, Y_Position = 400, MAC = "00:00:00:00:00:00" });
            _espDevices.Add(new Device() { X_Position = 400, Y_Position = 400, MAC = "00:00:00:00:00:00" });

            foreach (Device d in _devices)
            {
                if (d.X_Position > _mapWidth) _mapWidth = d.X_Position;
                if (d.Y_Position > _mapHeight) _mapHeight = d.Y_Position;
            }
            foreach (Device d in _espDevices)
            {
                if (d.X_Position > _mapWidth) _mapWidth = d.X_Position;
                if (d.Y_Position > _mapHeight) _mapHeight = d.Y_Position;
            }

            _points = new ObservableCollection<Point>();
            _points.Add(new Point(2, 100));
            _points.Add(new Point(3, 180));
            _points.Add(new Point(4, 170));
            _points.Add(new Point(5, 100));
            _points.Add(new Point(6, 140));
            _points.Add(new Point(7, 140));
            _points.Add(new Point(8, 187));
            _points.Add(new Point(9, 192));
            _points.Add(new Point(10, 176));
            _points.Add(new Point(11, 140));
            _points.Add(new Point(12, 110));
            _points.Add(new Point(13, 100));
            _points.Add(new Point(14, 100));
            _points.Add(new Point(15, 120));
            _points.Add(new Point(16, 150));
            _points.Add(new Point(17, 136));
            _points.Add(new Point(18, 186));
            _points.Add(new Point(19, 194));
            _points.Add(new Point(20, 136));
            _points.Add(new Point(21, 136));
            #endregion
        }
        #endregion

        #region Public Properties
        public ObservableCollection<Device> Devices => _devices;
        public ObservableCollection<Device> ESPDevices => _espDevices;
        public ObservableCollection<Point> Points
        {
            get => _points;
            set
            {
                _points = value;
                OnPropertyChanged(nameof(Points));
            }
        }
        public Double MapWidth => _mapWidth + 20; //added offset for device size
        public Double MapHeight => _mapHeight + 20;
        #endregion
    }
}
