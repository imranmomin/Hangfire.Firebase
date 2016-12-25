using System;

namespace Hangfire.Firebase
{
    public class FirebaseDistributedLockException : Exception
    {
        public FirebaseDistributedLockException(string message) : base(message)
        {
        }
    }
}
