using System.Threading;
using System.Windows.Threading;
using ProbeSniffer.Views;
using ProbeSniffer.Windows;
using ProbeSniffer.ViewModels.SplashScreen;
using System;

namespace ProbeSniffer
{
    public class SplashScreen
    {
        private Thread SplashThread = null;
        private SplashScreenWindow splashWindow = null;
        
        /// <summary>
        /// Shows the Splash Screen
        /// </summary>
        public void Show()
        {
            SplashThread = new Thread(new ThreadStart(() =>
            {
                splashWindow = new SplashScreenWindow();
                splashWindow.Show();
                Dispatcher.Run();

            }));
            SplashThread.Priority = ThreadPriority.AboveNormal;
            SplashThread.SetApartmentState(ApartmentState.STA);
            SplashThread.IsBackground = false;
            SplashThread.Start();
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Shows the ConfigurationLoading view in the Splash Screen
        /// </summary>
        public void ShowConfLoadingSplashScreen()
        {
            Dispatcher dispatcher = Dispatcher.FromThread(SplashThread);
            if (dispatcher == null) throw new Exception("SplashScreen Dispatcher not running");
            dispatcher.Invoke(() =>
            {
                ((SplashViewModel)splashWindow.DataContext).CurrentPage= new ConfigurationLoadingView();
            });
            
        }

        /// <summary>
        /// Shows the DatabaseConnection view in the Splash Screen
        /// </summary>
        public void ShowDBConneLoadingSplashScreen()
        {
            Dispatcher dispatcher = Dispatcher.FromThread(SplashThread);
            if (dispatcher == null) throw new Exception("SplashScreen Dispatcher not running");
            dispatcher.Invoke(() =>
            {
                ((SplashViewModel)splashWindow.DataContext).CurrentPage = new DatabaseConnectionLoadingView();
            });
        }

        public void Close()
        {
            SplashThread.Abort();
        }

        /// <summary>
        /// Shows the DeviceAwaiting view in the Splash Screen
        /// </summary>
        public void ShowDeviceAwaitingSplashScreen()
        {
            Dispatcher dispatcher = Dispatcher.FromThread(SplashThread);
            if (dispatcher == null) throw new Exception("SplashScreen Dispatcher not running");
            dispatcher.Invoke(() =>
            {
                ((SplashViewModel)splashWindow.DataContext).CurrentPage = new DevicesAwaitingView();
            });
        }
    }
}
