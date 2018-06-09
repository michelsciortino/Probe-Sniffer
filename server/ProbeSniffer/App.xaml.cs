﻿using Core.DataCollection;
using Core.DBConnection;
using Core.DeviceCommunication;
using Core.Models;
using ProbeSniffer.Windows;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ProbeSniffer
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {/*
        private Configuration configuration = null;
        private DataCollector dataCollector = null;
        private CancellationTokenSource broadcasterTokenS = null;
        private System.Windows.Forms.NotifyIcon notifyIcon = null;


        #region Windows
        private SplashScreen splash = null;
        private DataVisualizer visualizer = null;
        private ToastMenu toast = null;
        #endregion
        */
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Program program = new Program();
            program.Main();
        }

        /*
        protected new void Run()
        {
            //Showing Splash Screen
            splash = new SplashScreen();
            splash.Show();

            //Loading configuration
            splash.ShowConfLoadingSplashScreen();
            /*configuration = Configuration.LoadConfiguration();

            if (configuration is null)
            {
                ShowErrorMessage("Unable to load the configuration...\nExiting.");
                splash.Close();
                try { App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown; } catch { }
                App.Current.Shutdown();
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
                try { App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown; } catch { }
                App.Current.Shutdown();
                return;
            }

            //Connecting to Devices
            splash.ShowDeviceAwaitingSplashScreen();
            broadcasterTokenS = new CancellationTokenSource();
            UdpBroadcaster.Start(broadcasterTokenS.Token);
            dataCollector = new DataCollector();
            while (dataCollector.Initialize() is false) ;
            dataCollector.StartDataCollection();
            
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
        private void Toast_ShowGraphClicked(object sender, RoutedEventArgs e) => visualizer.Show();

        private new void Exit(object sender, EventArgs e)
        {
            toast.Close();
            visualizer.Close();
            broadcasterTokenS.Cancel();
            notifyIcon.Visible = false;
            try { App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown; } catch { }
            App.Current.Shutdown();
            base.OnExit(null);
        }

        private void MenuFlyout_Deactivated(object sender, EventArgs e) => toast.Hide();

        private void Toast_MouseDoubleClick(object sender, MouseButtonEventArgs e) => toast.Hide();

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (toast is null) return;
            toast.Left = SystemParameters.WorkArea.Width - toast.Width;
            toast.Top = SystemParameters.WorkArea.Height - toast.Height;
            toast.Opacity = 0;
            toast.Show();
            toast.Activate();
        }
        */
    }
}