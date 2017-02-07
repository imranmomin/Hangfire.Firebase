using System;
using Hangfire.Firebase;

namespace Hangfire
{
    /// <summary>
    /// Extensions methods for FirebaseStorage configuration
    /// </summary>
    public static class FirebaseStorageExtensions
    {
        /// <summary>
        /// Enables to attache FirebaseStorage to Hangfire
        /// </summary>
        /// <param name="configuration">The IGlobalConfiguration object</param>
        /// <param name="url">The url string to Firebase Database</param>
        /// <param name="authSecret">The secret key for the Firebase Database</param>
        /// <returns></returns>
        public static IGlobalConfiguration<FirebaseStorage> UseFirebaseStorage(this IGlobalConfiguration configuration, string url, string authSecret)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(authSecret)) throw new ArgumentNullException(nameof(authSecret));

            FirebaseStorage storage = new FirebaseStorage(url, authSecret);
            return configuration.UseStorage(storage);
        }

        /// <summary>
        /// Enables to attache FirebaseStorage to Hangfire
        /// </summary>
        /// <param name="configuration">The IGlobalConfiguration object</param>
        /// <param name="url">The url string to Firebase Database</param>
        /// <param name="authSecret">The secret key for the Firebase Database</param>
        /// <param name="options">The FirebaseStorage object to override any of the options</param>
        /// <returns></returns>
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
