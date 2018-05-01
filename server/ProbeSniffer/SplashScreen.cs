using System.Threading;
using System.Windows.Threading;
using ProbeSniffer.Views;
using ProbeSniffer.ViewModels;
using ProbeSniffer.Windows;

namespace ProbeSniffer
{
    public class SplashScreen
    {
        private Thread SplashThread = null;
        private SplashWindow splashWindow = null;
        
        /// <summary>
        /// Shows the Splash Screen
        /// </summary>
        public void Show()
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

        /// <summary>
        /// Shows the ConfigurationLoading view in the Splash Screen
        /// </summary>
        public void ShowConfLoadingSplashScreen()
        {
            Dispatcher dispatcher = Dispatcher.FromThread(SplashThread);
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
            Dispatcher.FromThread(SplashThread).Invoke(() =>
            {
                ((SplashViewModel)splashWindow.DataContext).CurrentPage = new DatabaseConnectionLoadingView();
            });
        }

        /// <summary>
        /// Shows the DeviceAwaiting view in the Splash Screen
        /// </summary>
        public void ShowDeviceAwaitingSplashScreen()
        {
            Dispatcher.FromThread(SplashThread).Invoke(() =>
            {
                ((SplashViewModel)splashWindow.DataContext).CurrentPage = new DevicesAwaitingView();
            });
        }
    }
}
