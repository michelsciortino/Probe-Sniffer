using Core.ViewModelBase;
using ProbeSniffer.Views.DataVisualizer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProbeSniffer.ViewModels.DataVisualizer
{
    public class DataVisualizationWindowViewModel : BaseViewModel
    {
        #region Private
        private const string AboutMessage = "Created by:\n\tMichel Sciortino\n\tAndrea Mora\n\tAntonio Monteanni\n\nDesigned by Michel Sciortino.";
        private const string AboutCaption = "Probe Sniffer";


        private Page _currentPage = null;
        private LiveViewViewModel _liveViewVM = null;
        private StatisticsViewModel _statisticsVM = null;
        private HiddenDevicesViewModel _hiddenDevicesVM = null;
        private SliderViewViewModel _sliderViewVM = null;
        #endregion

        #region Constructor
        public DataVisualizationWindowViewModel()
        {
            _liveViewVM = new LiveViewViewModel();
            _statisticsVM = new StatisticsViewModel();
            _hiddenDevicesVM = new HiddenDevicesViewModel();
            _sliderViewVM = new SliderViewViewModel();
            _currentPage = new LiveViewView(_liveViewVM);
        }
        #endregion

        #region Public Properties
        public Page CurrentPage {
            get => _currentPage;
            set
            {
                if (_currentPage == value)
                    return;
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
            }
        }

        #endregion

        #region Private Commands
        private ICommand _selectionCommand = null;
        private ICommand _aboutCommand = null;
        #endregion

        #region Public Commands
        public ICommand SelectionCommand => _selectionCommand ??
                                            (_selectionCommand = new RelayCommand<object>((x) => Navigate((SelectionChangedEventArgs)x)));
        public ICommand AboutCommand => _aboutCommand ??
                                        (_aboutCommand = new RelayCommand<object>((x) => MessageBox.Show(AboutMessage, AboutCaption, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None,MessageBoxOptions.ServiceNotification)));
        #endregion

        #region Private Methods
        private void Navigate(SelectionChangedEventArgs destination)
        {
            switch (((ListBoxItem)((ListBox)destination.Source).SelectedItem).Tag)
            {
                case "liveview":
                    CurrentPage = new LiveViewView(_liveViewVM);
                    break;
                case "statistics":
                    CurrentPage = new StatisticsView(_statisticsVM);
                    break;
                case "hiddendevices":
                    CurrentPage = new HiddenDevicesView(_hiddenDevicesVM);
                    break;
                case "sliderview":
                    CurrentPage = new SliderViewView(_sliderViewVM);
                    break;
            }
            switch (destination)
            {
                default: break;
            }
        }
        #endregion
    }
}
