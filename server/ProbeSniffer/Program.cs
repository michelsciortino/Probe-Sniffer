using System.Threading;
using Core.Models;
using Core.DBConnection;
using System.Windows;
using Core.DeviceCommunication;

namespace ProbeSniffer
{
    public class Program
    {
        private Configuration configuration = null;
        private DatabaseConnection dbConnection = null;
        private DeviceConnectionManager deviceCommunication = null;

        #region Windows
        private SplashScreen splash = null;
        private DataVisualizer visualizer = null;
        #endregion

        /// <summary>
        /// Main entry point of the program
        /// </summary>
        public void Main()
        {
            bool result = false;

            //Showing Splash Screen
            splash = new SplashScreen();
            splash.Show();

            //Loading configuration
            splash.ShowConfLoadingSplashScreen();
            configuration = Configuration.LoadConfiguration();

            if (configuration is null)
            {
                ShowErrorMessage("Unable to load the configuration...\nExiting.");
                splash.Close();
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Application.Current.Shutdown();
                return;
            }

            //Testing Database connection
            splash.ShowDBConneLoadingSplashScreen();
            dbConnection = new DatabaseConnection();
            int tries = 0;
            while (tries != 3)
            {
                dbConnection.Connect();
                if (dbConnection.Connected) break;
                Thread.Sleep(2000);
                tries++;
            }
            if (!dbConnection.Connected)    //not connected after 3 tries
            {
                ShowErrorMessage("Unable to connect to Database...\nExiting.");
                splash.Close();
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Application.Current.Shutdown();
                return;
            }


            //Connecting to Devices
            splash.ShowDeviceAwaitingSplashScreen();
            deviceCommunication = new DeviceConnectionManager();
            result = deviceCommunication.Initialize(configuration.Devices);

            if (result is false)
            {
                ShowErrorMessage("Unable to initialize ESP32 devices...\nExiting.");
                splash.Close();
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Application.Current.Shutdown();
                return;
            }

            //Opening visualizer
            splash.Close();
            visualizer = new DataVisualizer(configuration.Devices);
            visualizer.Show();
        }
        
        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message,
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error,
                            MessageBoxResult.None,
                            MessageBoxOptions.DefaultDesktopOnly);
        }

    }
}
