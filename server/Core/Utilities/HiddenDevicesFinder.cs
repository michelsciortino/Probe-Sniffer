using Core.Models;
using Core.Models.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Utilities
{
    public static class HiddenDevicesFinder
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
        /// <param name="entries">List of Probes</param>
        /// <returns>Return a List of HiddenDeviceInfo that contains for each device a list of equal Mac </returns>
        public static List<HiddenDeviceInfo> Find(List<Probe> entries)
        {
            List<HiddenDeviceInfo> hiddenDeviceList = new List<HiddenDeviceInfo>();
            Dictionary<int, HiddenDeviceInfo> hiddenDeviceDictionary = new Dictionary<int, HiddenDeviceInfo>();

            Dictionary<String, ProbeInterval> devicesInfoList = new Dictionary<string, ProbeInterval>();
            float[][] adj = null;
            int[] sol = null;

            foreach (Probe p in entries) //trovo i mac local e li salvo a gruppi
            {
                byte[] bytes = Encoding.ASCII.GetBytes(p.Sender.MAC);
                var bits = new BitArray(bytes);

                //DisplayBitArray(bits);
                if (bits[1] is true)
                {
                    if (devicesInfoList.ContainsKey(p.Sender.MAC))
                    {
                        devicesInfoList[p.Sender.MAC].SsidList.Add(p.SSID);
                        if (devicesInfoList[p.Sender.MAC].Start > p.Timestamp)
                            devicesInfoList[p.Sender.MAC].Start = p.Timestamp;
                        if (devicesInfoList[p.Sender.MAC].End < p.Timestamp)
                            devicesInfoList[p.Sender.MAC].End = p.Timestamp;
                    }
                    else
                    {
                        ProbeInterval probeInt = new ProbeInterval
                        {
                            Mac = p.Sender.MAC,
                            Start = p.Timestamp,
                            End = p.Timestamp,
                            SsidList=new List<string>()
                        };
                        probeInt.SsidList.Add(p.SSID);
                        devicesInfoList.Add(p.Sender.MAC, probeInt);
                    }

                }
            }
            Dictionary<int, String> MacId = new Dictionary<int, string>();  //ID - MAC

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
                    if (r != c)
                        adj[r][c] = GetDegreeOfSimilarity(probe_ext_r, probe_int_c);  //Match all probeInterval, return percentage of similarity
                    c++;
                }
                MacId.Add(r, probe_ext_r.Key);
                r++;
            }

            int cc = 0;
            // finding connected components
            for (int i = 0; i < devicesInfoList.Count; i++)
            {
                if (sol[i] == -1)
                {
                    CCFind(i, adj, ref sol, cc);
                    cc++;
                }
            }

            //Create Lists of equal mac
            for (int i = 0; i < sol.Length; i++)
            {
                if (sol[i] != -1)
                {
                    if (hiddenDeviceDictionary.ContainsKey(sol[i]))
                    {
                        if(!hiddenDeviceDictionary[sol[i]].MacList.Contains(MacId[i]))
                            hiddenDeviceDictionary[sol[i]].MacList.Add(MacId[i]);
                        foreach(var ssid in devicesInfoList[MacId[i]].SsidList)
                        {
                            if (!hiddenDeviceDictionary[sol[i]].SsidList.Contains(ssid))
                                hiddenDeviceDictionary[sol[i]].SsidList.Add(ssid);
                        }
                    }
                    else
                    {
                        HiddenDeviceInfo hd = new HiddenDeviceInfo
                        {
                            Id = i,
                            MacList = new HashSet<string>(),
                            SsidList=new HashSet<string>()
                        };
                        hd.MacList.Add(MacId[i]);
                        foreach (var ssid in devicesInfoList[MacId[i]].SsidList)
                        {
                            if (!hd.SsidList.Contains(ssid))
                                hd.SsidList.Add(ssid);
                        }
                        hiddenDeviceDictionary.Add(sol[i], hd);
                    }
                    sol[i] = -1;
                }
            }

            return hiddenDeviceDictionary.Values.ToList();
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
                return (float)mutui / (float)(probe_ext.Value.SsidList.Count + probe_int.Value.SsidList.Count - mutui);
            }
        }

        /// <summary>
        /// Finding connected components 
        /// </summary>
        /// <param name="root">Root node</param>
        /// <param name="adj">Adjacencies matrix</param>
        /// <param name="sol">Vector of solutions</param>
        /// <param name="cc">Connected component</param>
        private static void CCFind(int root, float[][] adj, ref int[] sol, int cc)
        {
            sol[root] = cc;
            List<KeyValuePair<float, int>> descendentPeers = new List<KeyValuePair<float, int>>();

            for (int i = 0; i < sol.Length; i++)
                if (adj[root][i] >= THRESHOLD)
                    descendentPeers.Add(new KeyValuePair<float,int>(adj[root][i], i));
            descendentPeers.Sort((a, b) => (a.Key < b.Key)?1:0);
            foreach (KeyValuePair<float, int> pair in descendentPeers)
            {
                int peer = pair.Value;
                if (sol[peer] == -1 && adj[peer][root] >= THRESHOLD)
                {
                    CCFind(peer, adj, ref sol, cc);
                }
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
    }
}
