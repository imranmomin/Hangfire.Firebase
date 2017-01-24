using System;

namespace Hangfire.Firebase.Entities
{
    internal class FireEntity : IExpireEntity
    {
        public DateTime? ExpireOn { get; set; }
    }
}
