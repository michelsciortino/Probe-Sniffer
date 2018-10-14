using System;
using System.Collections;
using Core.Models;
using Core.DBConnection;
using Core.Models.Database;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace DataGenerator
{
    class Program
    {
        public static string[] Macs ={  "ff:63:ae:97:15:8b",
                                        "83:57:b1:1f:97:1a",
                                        "fb:7c:3a:a2:cd:dd",
                                        "37:46:1f:38:9b:23",
                                        "bd:90:63:fb:56:8f",
                                        "32:8a:75:f7:ea:1c",
                                        "37:d7:54:32:75:c4",
                                        "e3:a1:1b:77:2e:e0",
                                        "06:9e:84:6b:8e:5b",
                                        "58:ab:d7:33:04:c9",
                                        "94:bf:3a:ea:31:8f",
                                        "c5:c8:85:b6:a6:7e",
                                        "6b:bd:fd:d5:09:fc",
                                        "94:de:3a:c0:66:a6",
                                        "4b:07:77:5f:98:2f",
                                        "17:77:28:3d:31:95",
                                        "44:ec:db:f8:6a:05",
                                        "d6:4f:8c:8a:d5:8c",
                                        "0e:a0:27:eb:d3:86",
                                        "75:0f:1a:2c:a2:f6",
                                        "78:59:01:5d:7d:32",
                                        "7b:fc:4d:2d:36:cc",
                                        "a2:e0:3f:7c:78:f1",
                                        "12:3b:8e:07:7f:10",
                                        "d8:b6:8a:a5:17:15",
                                        "de:2a:3b:d6:cd:89",
                                        "5b:80:df:3c:89:11",
                                        "3e:6b:5b:80:58:2c",
                                        "64:34:d0:dc:01:23",
                                        "39:1e:87:ae:50:16",
                                        "60:4e:31:ba:a4:42",
                                        "b1:97:cc:24:10:07",
                                        "a2:53:cf:3b:ed:b9"};

        public static string[] ssids = { "eduroam", "polito", "dlink", "fastweb", "vodafone" };

        const int DAYS = 38;

        static void Main(string[] args)
        {

            List<ESP32_Device> esps = new List<ESP32_Device>()
            {
                new ESP32_Device{Active=true,MAC="AA:AA:AA:AA:AA:AA",X_Position=0,Y_Position=0},
                new ESP32_Device{Active=true,MAC="BB:BB:BB:BB:BB:BB",X_Position=400,Y_Position=800},
                new ESP32_Device{Active=true,MAC="CC:CC:CC:CC:CC:CC",X_Position=800,Y_Position=0},
            };

            DatabaseConnection databaseConnection = new DatabaseConnection();
            databaseConnection.Connect();

            Random rand = new Random(23412321);
            int ID = 0;

            var crypt = new System.Security.Cryptography.SHA256Managed();
            DateTime date = DateTime.UtcNow;
            Console.WriteLine("Current time: " + date.ToShortDateString());
            Console.WriteLine("Press a key to start");
            Console.ReadKey();
            date=date.AddMilliseconds(-date.Millisecond);
            date=date.AddSeconds(-date.Second);
            for (int day=0;day< 5; day++)
            {
                for(int hour = 0; hour < 24; hour++)
                {
                    for(int minute = 0; minute < 60; minute++)
                    {
                        date=date.AddMinutes(1);
                        int limit = (int)(10 -((uint)rand.Next() %5) + ((uint)rand.Next()%20)) % 30;
                        ProbesInterval n_i = new Core.Models.Database.ProbesInterval
                        {
                            ActiveEsps = esps,
                            IntervalId = ID++,
                            Probes = new List<Probe>(),Timestamp=date
                        };

                        for (int i = 0; i < limit; i++)// generating device position
                        {
                            Probe p = new Probe
                            {
                                Sender = new Device { MAC = Macs[(uint)rand.Next() % Macs.Length], X_Position = (uint)rand.Next() % 800, Y_Position = (uint)rand.Next() % 800 },
                                SSID = ssids[(uint)rand.Next() % ssids.Length],
                                Timestamp = date
                            };
                            p.Hash = sha256(p.Sender.MAC + p.Timestamp.ToShortDateString() + p.SSID);
                            n_i.Probes.Add(p);                            
                        }
                        
                        if(databaseConnection.StoreProbesIntervalAsync(n_i).Result is false)
                        {
                            Console.WriteLine("unable to store interval");
                            Console.ReadLine();
                        }
                        /*else
                        {
                            //Console.WriteLine($"stored new Interval with {n_i.Probes.Count} probes at {n_i.Timestamp.ToShortTimeString()}");
                        }*/
                    }
                }
            }

        }


        static string sha256(string tobehashed)
        {
            var crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(tobehashed));
            foreach (byte theByte in crypto)
                hash += theByte.ToString("x2");
            return hash;
        }
    }
}
