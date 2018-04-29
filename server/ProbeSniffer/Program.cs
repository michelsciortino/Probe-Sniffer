using ProbeSniffer.ViewModels;
using ProbeSniffer.Windows;
using ProbeSniffer.Views;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Core.Models;

namespace ProbeSniffer
{
    public class Program
    {
        private SplashViewModel splashViewModel = null;
        private DevicesAwaitingViewModel devicesAwaitingViewModel = null;
        private SplashWindow splashWindow = null;


        private ObservableCollection<Device> _devices = null;


        public async Task MainAsync()
        {
            _devices = new ObservableCollection<Device>
            {
                new Device { Active = true },
                new Device { Active = true },
                new Device { Active = true },
                new Device { Active = false },
                new Device { Active = false }
            };


            splashViewModel = new SplashViewModel();

            ShowSplashScreen();

            ShowDBConneLoadingSplashScreen();

            await Task.Run(async () => Thread.Sleep(5000));

            ShowDeviceAwaitingSplashScreen();

        }

        public void ShowSplashScreen()
        {
            ShowConfLoadingSplashScreen();
            splashWindow = new SplashWindow(splashViewModel);
            splashWindow.Show();
        }


        public void ShowConfLoadingSplashScreen()
        {
            splashViewModel.CurrentPage = new ConfigurationLoadingView();
        }
        public void ShowDBConneLoadingSplashScreen()
        {
            splashViewModel.CurrentPage = new DatabaseConnectionLoadingView();
        }

        public void ShowDeviceAwaitingSplashScreen()
        {
            devicesAwaitingViewModel = new DevicesAwaitingViewModel(_devices);
            splashViewModel.CurrentPage = new DevicesAwaitingView(devicesAwaitingViewModel);
        }

        

    }
}
