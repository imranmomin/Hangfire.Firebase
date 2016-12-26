using System;

namespace Hangfire.Firebase.Entities
{
    internal interface IExpireEntity
    {
        DateTime? ExpireOn { get; set; }
    }
}
