using System.Threading;
using Core.Models;
using Core.DBConnection;
using Core.DeviceCommunication;
using System.Windows;
using Core.DataCollection;

namespace ProbeSniffer
{
    public class Program
    {
        private Configuration configuration = null;
        private DataCollector dataCollector = null;
        //private DeviceConnectionManager deviceCommunication = null;

        #region Windows
        private SplashScreen splash = null;
        private DataVisualizer visualizer = null;
        #endregion

        /// <summary>
        /// Main entry point of the program
        /// </summary>
        public void Main()
        {
            //bool result = false;

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
                try { Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown; } catch { }
                Application.Current.Shutdown();
                return;
            }
            ESPManager.Initialize(configuration.Devices);

            
            //Testing Database connection
            splash.ShowDBConneLoadingSplashScreen();
            DatabaseConnection dbConnection = new DatabaseConnection();
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
                try { Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown; } catch { }
                Application.Current.Shutdown();
                return;
            }

            //Connecting to Devices
            splash.ShowDeviceAwaitingSplashScreen();
            dataCollector.Initialize();
            dataCollector.StartDataCollection();

            //Opening visualizer
            splash.Close();
            
            visualizer = new DataVisualizer(configuration?.Devices);
            visualizer.Show();
        }
        
        private void ShowErrorMessage(string message)
        {
            Core.Controls.MessageBox errorBox = new Core.Controls.MessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
            errorBox.Show();
        }

    }
}
