using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;

using Hangfire.Storage;
using Hangfire.Logging;
using Hangfire.Firbase.Queue;

namespace Hangfire.Firbase
{
    public sealed class FirebaseStorage : JobStorage
    {
        private readonly string url;
        private readonly IFirebaseConfig config;

        public FirebaseStorageOptions Options { get; private set; }
        public PersistentJobQueueProviderCollection QueueProviders { get; private set; }

        public FirebaseStorage(string url, string authSecret) : this(url, new FirebaseStorageOptions { AuthSecret = authSecret }) { }

        public FirebaseStorage(string url, FirebaseStorageOptions options)
        {
            config = new FirebaseConfig
            {
                AuthSecret = options.AuthSecret,
                BasePath = url,
                RequestTimeout = options.RequestTimeout
            };

            this.url = url;
            Options = options;

            // prepare the schema
            if (options.PrepareSchema)
            {
                using (FirebaseClient client = new FirebaseClient(config))
                {

                }
            }
        }

        public override IStorageConnection GetConnection() => new FirebaseConnection(config);

        public override IMonitoringApi GetMonitoringApi() => new FirebaseMonitoringApi();

        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for Firebase job storage:");
            logger.Info($"     Firebase Url: {url}");
            logger.Info($"     Request Timeout: {Options.RequestTimeout}");
            logger.Info($"     Queue: {string.Join(",", Options.Queues)}");
        }

    }
}
