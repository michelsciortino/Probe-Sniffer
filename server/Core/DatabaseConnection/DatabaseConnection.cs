using Core.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Core.DatabaseConnection
{
    public class DatabaseConnection
    {
        private static Object ThreadSafeLock = new Object();
        private MongoClient client;
        private string DatabaseName = "ProbeSniffer";
        

        public bool Connected { get; set; } = false;

        public bool Connect()
        {
            client = new MongoClient(new MongoClientSettings
            {
                ConnectTimeout = new TimeSpan(0, 0, 10)
            });
            if (client.Cluster.Description.State == MongoDB.Driver.Core.Clusters.ClusterState.Disconnected)
                Connected = false;
            else
                Connected = true;
            return Connected;
        }

        private IMongoCollection<DatabaseEntry> GetCollection()
        {
            lock (ThreadSafeLock)
            {
                if (client.Cluster.Description.State is ClusterState.Disconnected) return null;
                return client.GetDatabase(DatabaseName).GetCollection<DatabaseEntry>("DeviceProbeLog");
            }
        }

        public IList<DatabaseEntry> GetAllData()
        {
            List<DatabaseEntry> entries = new List<DatabaseEntry>();
            try
            {
                IMongoCollection<DatabaseEntry> collection = GetCollection();
                if (collection is null) return entries;
                List<DatabaseEntry> new_entries = null;
                lock (ThreadSafeLock)
                {
                    new_entries = collection.Find(_ => true).ToList();
                }
                entries.AddRange(new_entries);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return entries;
        }

        public void Store(List<DatabaseEntry> entries)
        {
            lock (ThreadSafeLock)
            {
                try
                {
                    CancellationTokenSource source = new CancellationTokenSource();
                    client.StartSession(null, source.Token);
                    GetCollection().InsertManyAsync(entries);
                    source.Cancel();
                }catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }
    }
}
