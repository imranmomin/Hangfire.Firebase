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

            var storage = new FirebaseStorage(url, authSecret);
            return configuration.UseStorage(storage);
        }

        public static IGlobalConfiguration<FirebaseStorage> UseFirebaseStorage(this IGlobalConfiguration configuration, string url, FirebaseStorageOptions options)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var storage = new FirebaseStorage(url, options);
            return configuration.UseStorage(storage);
        }
    }
}
