using System;

namespace Hangfire.Firebase
{
    /// <summary>
    /// Represents errors that occur while acquiring a distributed lock.
    /// </summary>
    [Serializable]
    public class FirebaseDistributedLockException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the FirebaseDistributedLockException class with serialized data.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FirebaseDistributedLockException(string message) : base(message)
        {
        }
    }
}
