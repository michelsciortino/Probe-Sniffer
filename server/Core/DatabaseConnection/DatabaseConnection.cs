using Core.Models;
using Core.Models.Database;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DBConnection
{
    public class DatabaseConnection
    {
        private static Object ThreadSafeLock = new Object();
        private MongoClient client;
        private const string DatabaseName = "ProbeSniffer";
        private const string IntervalsCollectionName = "ProbesIntervals";
        private const string PacketsCollectionName = "Packets";
        private const string ESPsCollectionName = "ESPs";

        public bool Connected { get; set; } = false;

        public bool Connect()
        {
            client = new MongoClient(new MongoClientSettings
            {
                ConnectTimeout = new TimeSpan(0, 0, 10),
                ConnectionMode = ConnectionMode.Direct
            });
            if (client.Cluster.Description.State == MongoDB.Driver.Core.Clusters.ClusterState.Disconnected)
                Connected = false;
            else
                Connected = true;
            return Connected;
        }

        public bool TestConnection()
        {
            int tries = 0;
            while (tries != 3)
            {
                Connect();
                if (Connected) return true;
                Thread.Sleep(2000);
                tries++;
            }
            return false;
        }


        private IMongoCollection<Packet> GetPacketsCollection()
        {
            lock (ThreadSafeLock)
            {
                if (client.Cluster.Description.State is ClusterState.Disconnected) return null;
                return client.GetDatabase(DatabaseName).GetCollection<Packet>(PacketsCollectionName);
            }
        }
        private IMongoCollection<ProbesInterval> GetIntervalsCollection()
        {
            lock (ThreadSafeLock)
            {
                if (client.Cluster.Description.State is ClusterState.Disconnected) return null;
                return client.GetDatabase(DatabaseName).GetCollection<ProbesInterval>(IntervalsCollectionName);
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
        

        public async Task<bool> StorePacketAsync(Packet packet)
        {
            try { await GetPacketsCollection().InsertOneAsync(packet); }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                return false;
            }
            return true;
        }
        
        public async Task<bool> StoreProbesIntervalAsync(ProbesInterval interval)
        {
            try { await GetIntervalsCollection().InsertOneAsync(interval); }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
                return false;
            }
            return true;
        }

        public async Task<List<Packet>> GetRawData(DateTime start,DateTime end)
        {
            List<Packet> entries = new List<Packet>();
            try
            {
                entries.AddRange((await GetPacketsCollection().FindAsync((entry) => entry.Timestamp >= start && entry.Timestamp < end)).ToList());
            }
            catch (Exception ex) { Logger.Log(ex.Message); }
            return entries;
        }

        public async Task<ProbesInterval> GetLastInterval()
        {
            var options = new FindOptions<ProbesInterval>
            {
                Limit = 1,
                Sort = Builders<ProbesInterval>.Sort.Descending(p => p.Timestamp),
            };
            return (await GetIntervalsCollection().FindAsync(FilterDefinition<ProbesInterval>.Empty, options)).FirstOrDefault();
        }

        public async Task<List<ProbesInterval>> GetIntervalsBetween(DateTime start,DateTime end)
        {
            var options = new FindOptions<ProbesInterval>
            {
                Sort = Builders<ProbesInterval>.Sort.Ascending(p => p.Timestamp),
            };
            try
            {
                return (await GetIntervalsCollection().FindAsync((i) => i.Timestamp >= start && i.Timestamp <= end.AddMinutes(-1), options)).ToList();
            }
            catch { return null; }
        }

        public async Task<List<ProbesInterval>> GetLastNIntervals(int n)
        {
            var options = new FindOptions<ProbesInterval>
            {
                Limit = n,
                Sort = Builders<ProbesInterval>.Sort.Descending(i => i.Timestamp),
            };
            try
            {
                return (await GetIntervalsCollection().FindAsync(FilterDefinition<ProbesInterval>.Empty, options)).ToList();
            }
            catch { return null; }
        }

        public async Task<List<ProbesInterval>> GetLastIntervalsAfter(DateTime last_interval_timestamp)
        {
            var options = new FindOptions<ProbesInterval>
            {
                Sort = Builders<ProbesInterval>.Sort.Descending(i => i.Timestamp),
            };
            return (await GetIntervalsCollection().FindAsync((i) => i.Timestamp> last_interval_timestamp, options)).ToList();
        }
        
        public async Task<DateTime> GetFirstTimestamp()
        {
            var options = new FindOptions<Packet>
            {
                Limit = 1,
                Sort = Builders<Packet>.Sort.Descending(p => p.Timestamp),
            };
            var a = await GetPacketsCollection().FindAsync(FilterDefinition<Packet>.Empty, options);
            DateTime ret;
            try
            {
                ret=a.FirstOrDefault().Timestamp;
            }
            catch(Exception ex) {
                Thread.Sleep(10000);
                return default;
            }
            return ret;
        }
    }
}