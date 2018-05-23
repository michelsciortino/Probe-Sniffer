using Core.Models;
using Core.Models.Database;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DBConnection
{
    public class DatabaseConnection
    {
        private static Object ThreadSafeLock = new Object();
        private MongoClient client;
        private const string DatabaseName = "ProbeSniffer";
        private const string PacketsCollectionName = "Packets";
        private const string ESPsCollectionName = "ESPs";


        public bool Connected { get; set; } = false;

        public bool Connect()
        {
            client = new MongoClient(new MongoClientSettings
            {
                ConnectTimeout = new TimeSpan(0, 0, 10),
                ConnectionMode= ConnectionMode.Direct
            });
            if (client.Cluster.Description.State == MongoDB.Driver.Core.Clusters.ClusterState.Disconnected)
                Connected = false;
            else
                Connected = true;
            return Connected;
        }

        private IMongoCollection<Packet> GetPacketCollection()
        {
            lock (ThreadSafeLock)
            {
                if (client.Cluster.Description.State is ClusterState.Disconnected) return null;
                return client.GetDatabase(DatabaseName).GetCollection<Packet>(PacketsCollectionName);
            }
        }

        private IMongoCollection<Device> GetESPsCollection()
        {
            lock (ThreadSafeLock)
            {
                if (client.Cluster.Description.State is ClusterState.Disconnected) return null;
                return client.GetDatabase(DatabaseName).GetCollection<Device>(ESPsCollectionName);
            }
        }

        public async Task<List<Packet>> GetIntervalPacketAsync(DateTime start, DateTime end)
        {
            List<Packet> entries = new List<Packet>();
            entries.AddRange((await GetPacketCollection().FindAsync((entry) => entry.Timestamp >= start && entry.Timestamp < end)).ToList());
            return entries;
        }

        public async Task<ESP32_Device> GetESPPositionByTimestampAsync(string mac, DateTime timestamp)
        {
            Device entry;


            var options = new FindOptions<Device, Device>
            {
                Limit = 1,
                Sort = Builders<Device>.Sort.Descending(e => e.Timestamp),
            };

            entry = (await GetESPsCollection().FindAsync(e => e.MAC == mac && e.Timestamp < timestamp, options)).FirstOrDefault();
            return new ESP32_Device(entry);
        }

        public async Task<bool> StoreESPAsync(ESP32_Device entry)
        {
            try { await GetESPsCollection().InsertOneAsync(entry); }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public async Task<bool> StorePacketAsync(Packet packet)
        {
            try { await GetPacketCollection().InsertOneAsync(packet); }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public async Task<bool> StorePacketListAsync(List<Packet> packets)
        {
            try { await GetPacketCollection().InsertManyAsync(packets); }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

    }
}