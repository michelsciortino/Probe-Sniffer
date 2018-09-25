using Core.Models;
using Core.ViewModelBase;
using Ui.Views.DataVisualizer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ui.ViewModels.DataVisualizer
{
    public class DataVisualizationWindowViewModel : BaseViewModel
    {
        #region Private Members
        private const string AboutMessage = "Created by:\n\tMichel Sciortino\n\tAndrea Mora\n\tAntonio Monteanni\n\nDesigned by Michel Sciortino.";
        private const string AboutCaption = "Probe Sniffer";
        
        private LiveViewViewModel _liveViewVM = null;
        private readonly StatisticsViewModel _statisticsVM = null;
        private readonly HiddenDevicesViewModel _hiddenDevicesVM = null;
        private readonly SliderViewViewModel _sliderViewVM = null;
        private Thread logger_updater;
        private CancellationTokenSource loggerCancellation;
        #endregion

        #region Private Properties
        private Page _currentPage = null;
        private string _loggerText = "";
        private Visibility _loggerVisibility = Visibility.Collapsed;
        #endregion

        #region Constructor
        public DataVisualizationWindowViewModel()
        {
            _liveViewVM = new LiveViewViewModel();
            _statisticsVM = new StatisticsViewModel();
            _hiddenDevicesVM = new HiddenDevicesViewModel();
            _sliderViewVM = new SliderViewViewModel();
            _currentPage = new LiveViewView(_liveViewVM);
            _liveViewVM.RunUpdater();
            Queue<string> history=Logger.GetLogHistory();
            while(history.Count > 0) _loggerText += "\n" + history.Dequeue();
            loggerCancellation = new CancellationTokenSource();
            StartLogging();
        }
        
        public void StartLogging()
        {
            logger_updater = new Thread((x) => { UpdateLoggerText(loggerCancellation.Token); });
            logger_updater.Start();
        }

        public void StopLogging()
        {
            loggerCancellation.Cancel();
            logger_updater.Join();
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

        public string LoggerText
        {
            get => _loggerText;
        }

        public string LogButtonText
        {
            get => (_loggerVisibility == Visibility.Visible) ? "Hide Logger" : "Show Logger";
        }

        public Visibility LoggerVisibility
        {
            get => _loggerVisibility;
            set
            {
                if (_loggerVisibility == value)
                    return;
                _loggerVisibility = value;
                OnPropertyChanged(nameof(LoggerVisibility));
                OnPropertyChanged(nameof(LogButtonText));
            }
        }

        #endregion

        #region Private Commands
        private ICommand _selectionCommand = null;
        private ICommand _aboutCommand = null;
        private ICommand _showHideLoggerCommand;
        #endregion

        #region Public Commands
        public ICommand SelectionCommand => _selectionCommand ??
                                            (_selectionCommand = new RelayCommand<object>((x) => Navigate((SelectionChangedEventArgs)x)));
        public ICommand AboutCommand => _aboutCommand ??
                                        (_aboutCommand = new RelayCommand<object>((x) => ShowAbout()));
        public ICommand ShowHideLoggerCommand => _showHideLoggerCommand ??
                                                 (_showHideLoggerCommand = new RelayCommand<object>((x) => ShowHideLogger()));
        #endregion

        #region Private Methods
        private void Navigate(SelectionChangedEventArgs destination)
        {
            if(destination.GetType() != typeof(LiveViewView))
            {
                _liveViewVM.StopUpdater();
            }
            
            switch (((ListBoxItem)((ListBox)destination.Source).SelectedItem).Tag)
            {
                case "liveview":
                    CurrentPage = new LiveViewView(_liveViewVM);
                    _liveViewVM.RunUpdater();
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

        private void ShowAbout()
        {
            Core.Controls.MessageBox message = new Core.Controls.MessageBox(AboutMessage, AboutCaption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None);
            message.Show();
        }

        private void ShowHideLogger()
        {
            if (_loggerVisibility == Visibility.Visible)
                LoggerVisibility = Visibility.Collapsed;
            else
                LoggerVisibility = Visibility.Visible;
        }

        private void UpdateLoggerText(CancellationToken token)
        {
            while (token.IsCancellationRequested is false)
            {
                Logger.NewLogMessage.WaitOne(5000);
                Queue<string> new_messages = Logger.GetLogMessages();
                if (new_messages.Count == 0) continue;

                Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    while (new_messages.Count > 0)
                    {
                        _loggerText += "\n" + new_messages.Dequeue();
                    }
                    OnPropertyChanged(nameof(LoggerText));
                });
            }
        }
        #endregion
    }
}
