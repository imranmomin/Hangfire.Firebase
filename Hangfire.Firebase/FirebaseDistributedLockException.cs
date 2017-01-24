using System;

namespace Hangfire.Firebase
{
    [Serializable]
    public class FirebaseDistributedLockException : Exception
    {
        public FirebaseDistributedLockException(string message) : base(message)
        {
        }
    }
}
