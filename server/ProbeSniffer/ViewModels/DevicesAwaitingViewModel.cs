using Core.Models;
using Core.ViewModelBase;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeSniffer.ViewModels
{
    public class DevicesAwaitingViewModel : BaseViewModel
    {
        private ObservableCollection<Device> _items=null;
        public ObservableCollection<Device> Items
        {
            get =>_items;
            set { if (_items == value) return; _items = value; OnPropertyChanged(nameof(Items)); }
        }
        public DevicesAwaitingViewModel(ObservableCollection<Device> devices)
        {
            Items = devices;
        }
    }
}
