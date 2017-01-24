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

            using (new FirebaseDistributedLock(distributedLockKey, defaultLockTimeout, connection.Client))
            {
                FirebaseResponse response = connection.Client.Get("counters/raw");
                if (response.StatusCode == HttpStatusCode.OK && !response.IsNull())
                {
                    Dictionary<string, Dictionary<string, Counter>> counters = response.ResultAs<Dictionary<string, Dictionary<string, Counter>>>();
                    Array.ForEach(counters.Keys.ToArray(), key =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Dictionary<string, Counter> data;
                        if (counters.TryGetValue(key, out data))
                        {
                            data = data.ToDictionary(k => k.Key, v => v.Value);

                            int value = data.Sum(c => c.Value.Value);
                            DateTime? expireOn = data.Max(c => c.Value.ExpireOn);
                            Counter aggregated = new Counter
                            {
                                Value = value,
                                ExpireOn = expireOn
                            };

                            FirebaseResponse counterResponse = connection.Client.Get($"counters/aggregrated/{key}");
                            if (response.StatusCode == HttpStatusCode.OK && !counterResponse.IsNull())
                            {
                                Dictionary<string, Counter> collections = response.ResultAs<Dictionary<string, Counter>>();
                                if (collections != null)
                                {
                                    aggregated = collections.Values.FirstOrDefault();
                                    aggregated.Value += value;
                                    aggregated.ExpireOn = expireOn;
                                }
                            }

                            // update the aggregrated counter
                            FirebaseResponse aggResponse = connection.Client.Set($"counters/aggregrated/{key}", aggregated);
                            if (aggResponse.StatusCode == HttpStatusCode.OK)
                            {
                                // delete all the counter references for the key
                                Parallel.ForEach(data.Keys, reference => connection.Client.Delete($"counters/raw/{key}/{reference}"));
                            }
                        }
                    });

                    cancellationToken.WaitHandle.WaitOne(checkInterval);
                }

                Logger.Trace("Records from the 'Counter' table aggregated.");
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public override string ToString() => GetType().ToString();

    }
}
