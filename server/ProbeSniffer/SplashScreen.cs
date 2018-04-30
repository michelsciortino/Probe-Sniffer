using Core.Models;
using ProbeSniffer.ViewModels;
using ProbeSniffer.Views;
using ProbeSniffer.Windows;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;

namespace ProbeSniffer
{
    public class SplashScreen
    {
        private Thread SplashThread = null;
        private SplashWindow splashWindow = null;
        

        public void ShowSplashScreen()
        {
            SplashThread = new Thread(new ThreadStart(() =>
            {
                splashWindow = new SplashWindow();
                splashWindow.Show();
                Dispatcher.Run();
            }));
            SplashThread.Priority = ThreadPriority.AboveNormal;
            SplashThread.SetApartmentState(ApartmentState.STA);
            SplashThread.IsBackground = false;
            SplashThread.Start();
        }

        public void ShowConfLoadingSplashScreen()
        {
            Dispatcher dispatcher = Dispatcher.FromThread(SplashThread);
            dispatcher.Invoke(() =>
            {
                ((SplashViewModel)splashWindow.DataContext).CurrentPage= new ConfigurationLoadingView();
            });
            
        }
        public void ShowDBConneLoadingSplashScreen()
        {
            Dispatcher.FromThread(SplashThread).Invoke(() =>
            {
                ((SplashViewModel)splashWindow.DataContext).CurrentPage = new DatabaseConnectionLoadingView();
            });
        }

        public void ShowDeviceAwaitingSplashScreen(ObservableCollection<Device> devices)
        {
            Dispatcher.FromThread(SplashThread).Invoke(() =>
            {
                ((SplashViewModel)splashWindow.DataContext).CurrentPage = new DevicesAwaitingView();
            });
        }
    }
}
