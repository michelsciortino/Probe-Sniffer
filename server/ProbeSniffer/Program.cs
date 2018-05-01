using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Core.Models;

namespace ProbeSniffer
{
    public class Program
    {
        private static Configuration configuration = null;

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

            //splash = new SplashScreen();

            //splash.Show();

            //Loading configuration
            //splash.ShowConfLoadingSplashScreen();
            configuration = Configuration.LoadConfiguration();

            await Task.Run(async () => Thread.Sleep(5000));

            //Test Database connection
            //splash.ShowDBConneLoadingSplashScreen();
            /* connect()....*/

            await Task.Run(async () => Thread.Sleep(5000));

            //Connecting to Device
            //splash.ShowDeviceAwaitingSplashScreen();
            //DeviceCommunication.Initialize();

            await Task.Run(async () => Thread.Sleep(5000));

            //splash.Close();
            //visualizer = new DataVisualizer(_devices);
            //visualizer.Show();
        }

    }
}
