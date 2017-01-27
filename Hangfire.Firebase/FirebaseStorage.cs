using System.Collections.Generic;

using FireSharp.Config;
using Hangfire.Server;
using Hangfire.Storage;
using Hangfire.Logging;
using FireSharp.Interfaces;
using Hangfire.Firebase.Queue;

namespace Hangfire.Firebase
{
    public sealed class FirebaseStorage : JobStorage
    {
        private readonly string url;

        public IFirebaseConfig Config { get; }
        public FirebaseStorageOptions Options { get; }
        public PersistentJobQueueProviderCollection QueueProviders { get; }

        public FirebaseStorage(string url, string authSecret) : this(url, authSecret, new FirebaseStorageOptions()) { }

        public FirebaseStorage(string url, string authSecret, FirebaseStorageOptions options)
        {
            Config = new FirebaseConfig
            {
                AuthSecret = authSecret,
                BasePath = url,
                RequestTimeout = options.RequestTimeout,
                Serializer = new Json.JsonSerializer()
            };

            this.url = url;
            Options = options;

            JobQueueProvider provider = new JobQueueProvider(this);
            QueueProviders = new PersistentJobQueueProviderCollection(provider);
        }

        public override IStorageConnection GetConnection() => new FirebaseConnection(this);

        public override IMonitoringApi GetMonitoringApi() => new FirebaseMonitoringApi(this);

#pragma warning disable 618
        public override IEnumerable<IServerComponent> GetComponents()
#pragma warning restore 618
        {
            yield return new ExpirationManager(this);
            yield return new CountersAggregator(this);
        }

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for Firebase job storage:");
            logger.Info($"     Firebase Url: {url}");
            logger.Info($"     Request Timeout: {Options.RequestTimeout}");
            logger.Info($"     Counter Agggerate Interval: {Options.CountersAggregateInterval.TotalSeconds} seconds");
            logger.Info($"     Queue Poll Interval: {Options.QueuePollInterval.TotalSeconds} seconds");
            logger.Info($"     Expiration Check Interval: {Options.ExpirationCheckInterval.TotalSeconds} seconds");
            logger.Info($"     Queue: {string.Join(",", Options.Queues)}");
        }

        public override string ToString() => $"Firbase Database : {url}";

    }
}
