using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Core.Models;
using Core.DatabaseConnection;
using System.Windows;
using System.Collections.Generic;
using Core.DeviceCommunication;

namespace ProbeSniffer
{
    public class Program
    {
        private Configuration configuration = null;
        private DatabaseConnection DBconnection = null;
        private DeviceCommunication deviceCommunication = null;

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
            DBconnection = new DatabaseConnection();
            int tries = 0;
            while (tries != 3)
            {
                DBconnection.Connect();
                if (DBconnection.Connected) break;
                Thread.Sleep(2000);
                tries++;
            }
            if (!DBconnection.Connected)    //not connected after 3 tries
            {
                ShowErrorMessage("Unable to connect to Database...\nExiting.");
                splash.Close();
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Application.Current.Shutdown();
                return;
            }


            //Connecting to Devices
            splash.ShowDeviceAwaitingSplashScreen();
            deviceCommunication = new DeviceCommunication();
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
