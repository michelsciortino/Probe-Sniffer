using Core.Models;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Core.DeviceCominication.Messages;

namespace Core.DeviceCominication
{
    public static class DeviceCominication
    {
        public const int SERVER_PORT = 45445;
        public const int DEVICE_PORT = 45448;

        public static bool Initialize(IList<Device> devices) {
            bool result;
            Thread ServerAdvertismentThread = new Thread(() => { DoAdvertisement(); });
            result=ReceiveDeviceReadys(devices);
            if(result)
                foreach (Device d in devices)
                {
                    result = SendPacketToDevice(new Ok_Message().ToString(), d);
                    if (result == false); break;
                }
            ServerAdvertismentThread.Abort();
            return result;
        }


        public static void DoAdvertisement()
        {
            Server_Advertisement advertisement = new Server_Advertisement();
            IPAddress broadcast = LocalNetworkConnection.GetBroadcastAddress();

            UDPsender.Send(broadcast, SERVER_PORT, advertisement.ToString());
        }

        public static bool ReceiveDeviceReadys(IList<Device> devices)
        {

            return true;
        }

        /// <summary>
        /// Sends a packet to a device
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet"></param>
        /// <returns></returns>
        private static bool SendPacketToDevice(string packet, Device device)
        {


            return true;
        }

    }

    

}
