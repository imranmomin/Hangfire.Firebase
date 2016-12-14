using System;

namespace Hangfire.Firbase
{
    public class FirebaseStorageOptions
    {
        public string AuthSecret { get; set; }
        public TimeSpan? RequestTimeout { get; set; }
        public bool PrepareSchema { get; set; }

        public FirebaseStorageOptions()
        {
            RequestTimeout = TimeSpan.FromSeconds(30);
            PrepareSchema = true;
        }
    }
}
