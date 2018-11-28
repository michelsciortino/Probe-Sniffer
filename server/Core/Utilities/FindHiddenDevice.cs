using Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Utilities
{
    public static class FindHiddenDevice
    {
        private const float THRESHOLD = 0.6f;
        #region Private Classes
        private class ProbeInterval
        {
            public string Mac { get; set; }
            public List<String> SsidList { get; set; }
            public DateTime End { get; set; }
            public DateTime Start { get; set; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Find devices that hide their MAC
        /// </summary>
        /// <param name="entries">List of Packets</param>
        /// <returns>Return a List of HiddenDeviceInfo that contains for each device a list of equal Mac </returns>
        public static List<HiddenDeviceInfo> Find(List<Packet> entries)
        {
            List<HiddenDeviceInfo> hiddenDeviceList = new List<HiddenDeviceInfo>();
            Dictionary<String, ProbeInterval> devicesInfoList = new Dictionary<string, ProbeInterval>();
            float[][] adj = null;
            int[] sol = null;

            foreach (Packet p in entries) //trovo i mac local e li salvo a gruppi
            {
                byte[] bytes = Encoding.ASCII.GetBytes(p.MAC);
                var bits = new BitArray(bytes);

                //DisplayBitArray(bits);
                if (bits[1] is true)
                {
                    if (p.SSID == "dlinko" || p.SSID == "ESP32_AP")
                    {
                        if (devicesInfoList.ContainsKey(p.MAC))
                        {
                            devicesInfoList[p.MAC].SsidList.Add(p.SSID);
                            if (devicesInfoList[p.MAC].Start > p.Timestamp)
                                devicesInfoList[p.MAC].Start = p.Timestamp;
                            if (devicesInfoList[p.MAC].End < p.Timestamp)
                                devicesInfoList[p.MAC].End = p.Timestamp;
                        }
                        /*if (p.SSID == "dlinko")
                        Console.WriteLine(p.MAC + " " + p.SSID + " " + p.Timestamp);*/
                    }

                }
            }
            //ordino la lista in base al timestamp --> forse non serve
            //macLocal.Sort((pack1, pack2) => (pack1.Timestamp.CompareTo(pack2.Timestamp)));

            sol = new int[devicesInfoList.Count];
            for (int i = 0; i < devicesInfoList.Count; i++)
                sol[i] = -1;

            //matrix 
            adj = new float[devicesInfoList.Count][];
            for (int i = 0; i < devicesInfoList.Count; i++)
                adj[i] = new float[devicesInfoList.Count];

            int r = 0;
            foreach (var probe_ext_r in devicesInfoList)
            {
                int c = 0;
                foreach (var probe_int_c in devicesInfoList)
                {
                    if(r!=c)
                        adj[r][c] = GetDegreeOfSimilarity(probe_ext_r, probe_int_c);  //Match all probeInterval, return percentage of similarity
                    c++;
                }
                r++;
            }

            int cc = 0;
            // finding connected components
            for (int i=0;i< devicesInfoList.Count;i++)
            {
                if (sol[i] == -1)
                {
                    CCFind(i, adj,ref sol, cc);
                    cc++;
                }
            }
     
            return hiddenDeviceList;
        }

        private static void CCFind(int root,float[][] adj,ref int[] sol,int cc)
        {
            int maxPeer = -1;
            sol[root] = cc;
            SortedList<float,int> descendentPeers = new SortedList<float,int>();
            
            for(int i = 0; i < sol.Length; i++)
                if (adj[root][i] > THRESHOLD)
                    descendentPeers.Add(adj[root][i], i);
            
            foreach(KeyValuePair<float,int> pair in descendentPeers.Reverse())
            {
                int peer = pair.Value;
                if (sol[peer]!=-1 && adj[peer][root] > THRESHOLD)
                {
                    CCFind(maxPeer,adj,ref sol, cc);
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Calcolate percent of matching between two ProbeInterval
        /// </summary>
        /// <param name="probe_ext">First group of probe</param>
        /// <param name="probe_int">Second group of probe</param>
        /// <returns>percent of matching between two ProbeInterval, 0 if is not compatible</returns>
        private static float GetDegreeOfSimilarity(KeyValuePair<string, ProbeInterval> probe_ext, KeyValuePair<string, ProbeInterval> probe_int)
        {
            if (probe_ext.Value.End > probe_int.Value.Start)
                return 0;
            else
            {
                int i, j = 0, mutui = 0;
                for (i = 0; i < probe_int.Value.SsidList.Count && j < probe_ext.Value.SsidList.Count; i++)
                {
                    int h;
                    for (h = j + 1; h < probe_ext.Value.SsidList.Count; h++)
                    {
                        if (probe_ext.Value.SsidList[h] == probe_int.Value.SsidList[i])
                        {
                            mutui++;
                            j = h;
                            break;
                        }
                    }
                }
                return mutui / (probe_ext.Value.SsidList.Count + probe_int.Value.SsidList.Count - mutui);
            }
        }

        /// <summary>
        /// Display bits as 0s and 1s.
        /// </summary>
        private static void DisplayBitArray(BitArray bitArray)
        {
            for (int i = 0; i < bitArray.Count; i++)
            {
                bool bit = bitArray.Get(i);
                Console.Write(bit ? 1 : 0);
            }
            Console.WriteLine();
        }
        #endregion

        #region Old
        /* foreach (var device_ext in devicesInfoList)       
             {
                 if (mac == p.MAC)
                     {
                         equal[count] = p;
                         count++;
                     }
                     else
                     {
                         mac = p.MAC;
                         if (count > 1)
                         {
                             //controllo se ho match con quello che ho già salvato
                             int tempNumProbes = count;
                             string tempMac = equal[0].MAC;
                             List<String> tempSsids = new List<string>();
                             //List<int> tempSeqNum = new List<int>();
                             for (int i = 0; i < count; i++)
                             {
                                 tempSsids.Add(equal[i].SSID);
                                 //tempSeqNum.Add();    //inviare seqNumber
                             }
                             //check su ogni deviceHidden già salvato
                             foreach (HiddenDeviceInfo h in hiddenDeviceList)
                             {
                                 float ret = MatchDevice();      //check that return 0=false,1=true, 0-1=percentual of match
                                 if (ret == 1)
                                 {
                                     h.MacList.Add(tempMac);
                                 }

                             }
                         }
                         else
                         {
                             if (equal[0] != null)
                             {
                                 if (equal[0].MAC != mac)
                                 {
                                     for (int i = 0; i < count; i++)
                                     {
                                         equal[i] = null;
                                     }
                                     count = 1;
                                 }
                             }
                             equal[0] = p;
                             count = 1;
                         }
                     }
                 }*/
            #endregion
        }
    }
