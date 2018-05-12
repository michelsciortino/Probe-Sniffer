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

        private IMongoCollection<IntervalDataEntry> GetIntervalDataCollection()
        {
            lock (ThreadSafeLock)
            {
                if (client.Cluster.Description.State is ClusterState.Disconnected) return null;
                return client.GetDatabase(DatabaseName).GetCollection<IntervalDataEntry>("IntervalData");
            }
        }

        private IMongoCollection<IntervalDescriptionEntry> GetIntervalDescriptionCollection()
        {
            lock (ThreadSafeLock)
            {
                if (client.Cluster.Description.State is ClusterState.Disconnected) return null;
                return client.GetDatabase(DatabaseName).GetCollection<IntervalDescriptionEntry>("IntervalDescription");
            }
        }
        /*
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
        */
        public async Task<List<IntervalDataEntry>> GetLastIntervalDataEntriesAsync()
        {
            List<IntervalDataEntry> entries = new List<IntervalDataEntry>();

            IntervalDescriptionEntry last= await GetLastIntervalDescritpionEntryAsync();
            Debug.WriteLine("Max found: " + last.IntervalId);

            entries.AddRange((await GetIntervalDataCollection().FindAsync((entry) => entry.IntervalId == last.IntervalId)).ToList());

            return entries;
        }

        public async Task<IntervalDescriptionEntry> GetLastIntervalDescritpionEntryAsync()
        {
            IntervalDescriptionEntry entry;
            
            var options = new FindOptions<IntervalDescriptionEntry, IntervalDescriptionEntry>
            {
                Limit = 1,
                Sort = Builders<IntervalDescriptionEntry>.Sort.Descending(e => e.IntervalId)
            };

            entry = (await GetIntervalDescriptionCollection().FindAsync(FilterDefinition<IntervalDescriptionEntry>.Empty, options)).FirstOrDefault();
            return entry;
        }

        public async Task StoreIntervalDescriptionEntryAsync(IntervalDescriptionEntry entry)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            try
            {
                client.StartSession(null, source.Token);
                await GetIntervalDescriptionCollection().InsertOneAsync(entry);
                source.Cancel();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public async Task StoreIntervalDataEntriesAsync(List<IntervalDataEntry> entries)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            try
            {                
                client.StartSession(null, source.Token);
                await GetIntervalDataCollection().InsertManyAsync(entries);
                source.Cancel();
            }
            catch (Exception e)
            {
                source.Cancel();
                Debug.WriteLine(e.Message);
            }
        }
    }
}
