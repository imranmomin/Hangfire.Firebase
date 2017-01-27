using System;

namespace Hangfire.Firebase
{
    public class FirebaseStorageOptions
    {
        public TimeSpan? RequestTimeout { get; set; }
        public string[] Queues { get; set; }
        public TimeSpan ExpirationCheckInterval { get; set; }
        public TimeSpan CountersAggregateInterval { get; set; }
        public TimeSpan QueuePollInterval { get; set; }

        public FirebaseStorageOptions()
        {
            RequestTimeout = TimeSpan.FromSeconds(30);
            Queues = new[] { "default", "critical" };
            ExpirationCheckInterval = TimeSpan.FromMinutes(15);
            CountersAggregateInterval = TimeSpan.FromMinutes(1);
            QueuePollInterval = TimeSpan.FromSeconds(2);
        }
    }
}
