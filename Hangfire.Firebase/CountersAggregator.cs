using FireSharp.Response;
using Hangfire.Firebase.Entities;
using Hangfire.Logging;
using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Firebase
{
#pragma warning disable 618
    internal class CountersAggregator : IServerComponent
#pragma warning restore 618
    {
        private static readonly ILog Logger = LogProvider.For<CountersAggregator>();
        private const string distributedLockKey = "countersaggragator";
        private static readonly TimeSpan defaultLockTimeout = TimeSpan.FromMinutes(5);
        private readonly FirebaseConnection connection;
        private readonly TimeSpan checkInterval;

        public CountersAggregator(FirebaseStorage storage, TimeSpan checkInterval)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            this.connection = (FirebaseConnection)storage.GetConnection();
            this.checkInterval = checkInterval;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Logger.Debug("Aggregating records in 'Counter' table.");

            using (FirebaseDistributedLock @lock = new FirebaseDistributedLock(distributedLockKey, defaultLockTimeout, connection.Client))
            {
                FirebaseResponse respone = connection.Client.Get("counters/raw");
                if (respone.StatusCode == HttpStatusCode.OK)
                {
                    Dictionary<string, Dictionary<string, Counter>> counters = respone.ResultAs<Dictionary<string, Dictionary<string, Counter>>>();
                    string[] keys = counters.Select(k => k.Key).ToArray();

                    List<Task<bool>> tasks = new List<Task<bool>>();
                    Array.ForEach(keys, key =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Task<bool> task = Task.Run(() =>
                        {
                            Dictionary<string, Counter> data;
                            if (counters.TryGetValue(key, out data))
                            {
                                data = data.Where(c => c.Value.ExpireOn.HasValue && c.Value.ExpireOn < DateTime.UtcNow).ToDictionary(k => k.Key, v => v.Value);
                                if (data.Count > 0)
                                {
                                    int value = data.Sum(c => c.Value.Value);
                                    DateTime? expireOn = data.Max(c => c.Value.ExpireOn);
                                    Counter aggregated = new Counter { Value = value, ExpireOn = expireOn };

                                    FirebaseResponse response = connection.Client.Get($"counters/aggregrated/{key}");
                                    if (response.StatusCode == HttpStatusCode.OK)
                                    {
                                        aggregated = response.ResultAs<Counter>();
                                        aggregated.Value += value;
                                        aggregated.ExpireOn = expireOn;
                                    }

                                    // update the aggregrated counter
                                    response = connection.Client.Set($"counters/aggregrated/{key}", aggregated);
                                    if (respone.StatusCode == HttpStatusCode.OK)
                                    {
                                        // delete all the counter references
                                        string[] references = data.Select(c => c.Key).ToArray();
                                        Array.ForEach(references, reference =>
                                        {
                                            response = connection.Client.Delete($"counters/aggregrated/{key}/{reference}");
                                            Task.Delay(200);
                                        });
                                    }
                                }
                            }
                            return true;
                        });
                        tasks.Add(task);

                        cancellationToken.WaitHandle.WaitOne(checkInterval);
                    });
                    Task.WaitAll(tasks.ToArray());
                }
            }

            Logger.Trace("Records from the 'Counter' table aggregated.");
            cancellationToken.ThrowIfCancellationRequested();
        }

        public override string ToString()
        {
            return GetType().ToString();
        }
        
    }
}
