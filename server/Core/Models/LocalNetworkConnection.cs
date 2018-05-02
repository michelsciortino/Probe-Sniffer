using NativeWifi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Core.Models
{
    static class LocalNetworkConnection
    {

        public static IPAddress GetLocalIp()
        {
            IPAddress localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("1.1.1.1", 80);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address;
            }
            return localIP;
        }

        public static IPAddress GetNetmask()
        {
            IPAddress address = GetLocalIp();
            List<NetworkInterface> interfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();

            foreach (NetworkInterface i in interfaces)
            {
                List<UnicastIPAddressInformation> addresses = i.GetIPProperties().UnicastAddresses.ToList();
                foreach (UnicastIPAddressInformation inf in addresses)
                {
                    if (inf.Address.Equals(address))
                    {

                        return inf.IPv4Mask;
                    }
                }
            }
            return null;
        }

        public static IPAddress GetBroadcastAddress()
        {
            IPAddress address = GetLocalIp();
            IPAddress subnetMask = GetNetmask();

            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public static IList<string> GetConnectedWifiSSID()
        {
            List<String> connectedSsids = new List<string>();
            WlanClient wlan = new WlanClient();
            foreach (WlanClient.WlanInterface wlanInterface in wlan.Interfaces)
            {
                connectedSsids.Add(wlanInterface.CurrentConnection.profileName);
                /*Wlan.Dot11Ssid ssid = wlanInterface.CurrentConnection.wlanAssociationAttributes.dot11Ssid;
                connectedSsids.Add(new String(Encoding.ASCII.GetChars(ssid.SSID, 0, (int)ssid.SSIDLength)));*/
            }
            return connectedSsids;
        }
    }
}