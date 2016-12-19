using System;
using Hangfire.Firbase;

namespace Hangfire
{
    public static class FirebaseStorageExtensions
    {
        public static IGlobalConfiguration<FirebaseStorage> UseFirebaseStorage(this IGlobalConfiguration configuration, string url, string authSecret)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(authSecret)) throw new ArgumentNullException(nameof(authSecret));

            FirebaseStorage storage = new FirebaseStorage(url, authSecret);
            return configuration.UseStorage(storage);
        }

        public static IGlobalConfiguration<FirebaseStorage> UseFirebaseStorage(this IGlobalConfiguration configuration, string url, string authSecret, FirebaseStorageOptions options)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(authSecret)) throw new ArgumentNullException(nameof(authSecret));
            if (options == null) throw new ArgumentNullException(nameof(options));

            FirebaseStorage storage = new FirebaseStorage(url, authSecret, options);
            return configuration.UseStorage(storage);
        }
    }
}
