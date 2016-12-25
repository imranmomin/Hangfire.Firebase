using System;

namespace Hangfire.Firebase
{
    public class FirebaseStorageOptions
    {
        public TimeSpan? RequestTimeout { get; set; }
        public string[] Queues { get; set; }

        public FirebaseStorageOptions()
        {
            RequestTimeout = TimeSpan.FromSeconds(30);
            Queues = new[] { "default" };
        }
    }
}
