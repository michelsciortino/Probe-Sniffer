using System.Threading;
using Core.Models;
using Core.DBConnection;
using Core.DeviceCommunication;
using System.Windows;
using Core.DataCollection;
using System.IO;
using Server.Windows;
using System;
using System.Windows.Input;
using System.Reflection;

namespace Server
{
    public class Program
    {
        private Configuration configuration = null;
        private DataCollector dataCollector = null;
        private CancellationTokenSource broadcasterTokenS = null;
        private System.Windows.Forms.NotifyIcon notifyIcon = null;

        #region Windows
        private SplashScreen splash = null;
        private DataVisualizer visualizer = null;
        private ToastMenu toast = null;
        #endregion

        public void Main()
        {
            visualizer = new DataVisualizer();

            //Showing Splash Screen
            splash = new SplashScreen();
            splash.Show();

            //Loading configuration
            splash.ShowConfLoadingSplashScreen();
            configuration = Configuration.LoadConfiguration();
            if (configuration is null)
            {
                ShowErrorMessage("Unable to load the configuration...\nExiting.");
                Environment.Exit(0);
                return;
            }
            ESPManager.Initialize(configuration.Devices);

            //Testing Database connection
            splash.ShowDBConneLoadingSplashScreen();
            DatabaseConnection dbConnection = new DatabaseConnection();
            Thread.Sleep(1000);
            /*if (dbConnection.TestConnection() == false) //not connected after 3 tries
            {
                ShowErrorMessage("Unable to connect to Database...\nExiting.");
                Environment.Exit(0);
                return;
            }*/

            //Connecting to Devices
            splash.ShowDeviceAwaitingSplashScreen();
            broadcasterTokenS = new CancellationTokenSource();
            UdpBroadcaster.Start(broadcasterTokenS.Token);
            dataCollector = new DataCollector();
            /*dataCollector.Initialize();
            if(dataCollector.Initialized is false)
            {
                ShowErrorMessage("Unable to initialize devices...\nExiting.");
                Environment.Exit(0);
                return;
            }*/
            Thread.Sleep(1000);


            

            //Setting up the notification icon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            Stream iconStream = Assembly.GetAssembly(typeof(Core.Controls.MessageBox)).GetManifestResourceStream("Core.Resources.icon.ico");
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            notifyIcon.Visible = true;
            notifyIcon.Click += NotifyIcon_Click;
            toast = new ToastMenu();
            toast.MouseDoubleClick += Toast_MouseDoubleClick;
            toast.Deactivated += MenuFlyout_Deactivated;
            toast.ExitCLicked += Exit;
            toast.ShowGraphClicked += Toast_ShowGraphClicked;

            //starting data collection

            ShowMessage("Starting data collection");
            //dataCollector.StartDataCollection();
            
            //Opening visualizer
            splash.Close();
        }

        private void ShowErrorMessage(string message)
        {
            Core.Controls.MessageBox errorBox = new Core.Controls.MessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
            errorBox.Show();
        }

        private void ShowMessage(string message)
        {
            Core.Controls.MessageBox errorBox = new Core.Controls.MessageBox(message, "Probe Sniffer", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None);
            errorBox.Show();
        }

        private void Exit(object sender, EventArgs e)
        {
            toast.Close();
            visualizer.Close();
            broadcasterTokenS.Cancel();
            notifyIcon.Visible = false;
            try { Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown; } catch { }
            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        private void Toast_ShowGraphClicked(object sender, RoutedEventArgs e) => visualizer.Show();

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (toast is null) return;
            toast.Left = SystemParameters.WorkArea.Width - toast.Width;
            toast.Top = SystemParameters.WorkArea.Height - toast.Height;
            toast.Opacity = 0;
            toast.Show();
            toast.Activate();
        }

        private void MenuFlyout_Deactivated(object sender, EventArgs e) => toast.Hide();

        private void Toast_MouseDoubleClick(object sender, MouseButtonEventArgs e) => toast.Hide();
    }
}

