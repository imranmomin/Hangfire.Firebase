using System;
using System.Collections.Generic;

using FireSharp.Config;
using Hangfire.Server;
using Hangfire.Storage;
using Hangfire.Logging;
using FireSharp.Interfaces;
using Hangfire.Firebase.Queue;

namespace Hangfire.Firebase
{
    /// <summary>
    /// FirebaseStorage extend the storage option for Hangfire.
    /// </summary>
    public sealed class FirebaseStorage : JobStorage
    {
        private readonly string url;

        internal IFirebaseConfig Config { get; }

        internal FirebaseStorageOptions Options { get; }

        internal PersistentJobQueueProviderCollection QueueProviders { get; }

        /// <summary>
        /// Initializes the FirebaseStorage form the url & auth secret provide.
        /// </summary>
        /// <param name="url">The url string to Firebase Database</param>
        /// <param name="authSecret">The secret key for the Firebase Database</param>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="authSecret"/> argument is null.</exception>
        public FirebaseStorage(string url, string authSecret) : this(url, authSecret, new FirebaseStorageOptions()) { }

        /// <summary>
        /// Initializes the FirebaseStorage form the url & auth secret provide.
        /// </summary>
        /// <param name="url">The url string to Firebase Database</param>
        /// <param name="authSecret">The secret key for the Firebase Database</param>
        /// <param name="options">The FirebaseStorage object to override any of the options</param>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="authSecret"/> argument is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> argument is null.</exception>
        public FirebaseStorage(string url, string authSecret, FirebaseStorageOptions options)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(authSecret)) throw new ArgumentNullException(nameof(authSecret));
            if (options == null) throw new ArgumentNullException(nameof(options));

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
