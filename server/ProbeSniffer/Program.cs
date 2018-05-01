using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Core.Models;
using Core.DatabaseConnection;
using System.Windows;
using System.Collections.Generic;

namespace ProbeSniffer
{
    public class Program
    {
        private Configuration configuration = null;
        private DatabaseConnection DBconnection;

        #region Windows
        private SplashScreen splash = null;
        private DataVisualizer visualizer = null;
        #endregion
        
        private ObservableCollection<Device> _devices = null;

        /// <summary>
        /// Main entry point of the program
        /// </summary>
        public async void Main()
        {
            //testing
            _devices = new ObservableCollection<Device>
            {
                new Device { Active = true },
                new Device { Active = true },
                new Device { Active = true },
                new Device { Active = false },
                new Device { Active = false }
            };            

            splash = new SplashScreen();

            splash.Show();

            //Loading configuration
            splash.ShowConfLoadingSplashScreen();
            configuration = Configuration.LoadConfiguration();

            //await Task.Run(async () => Thread.Sleep(5000));

            //Test Database connection
            splash.ShowDBConneLoadingSplashScreen();
            DBconnection = new DatabaseConnection();
            int tries = 0;
            while (tries!=3)
            {
                DBconnection.Connect();
                if (DBconnection.Connected) break;
                //splash.ShowDBConnectionErrorRetrying();
                Thread.Sleep(2000);
                tries++;
            }

            if (!DBconnection.Connected)    //not connected after 3 tries
            {
                MessageBox.Show("Unable to connect to Database...\nExiting.", "Error", MessageBoxButton.OK,MessageBoxImage.Error,MessageBoxResult.None,MessageBoxOptions.DefaultDesktopOnly);
                splash.Close();
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Application.Current.Shutdown();
                return;
            }
            

            await Task.Run(async () => Thread.Sleep(5000));
            //Connecting to Device
            splash.ShowDeviceAwaitingSplashScreen();
            //DeviceCommunication.Initialize();

            await Task.Run(async () => Thread.Sleep(5000));

            splash.Close();
            //visualizer = new DataVisualizer(_devices);
            //visualizer.Show();
        }

    }
}
