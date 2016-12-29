using System;

namespace Hangfire.Firebase.Entities
{
    internal class Counter : FireEntity, IExpireEntity
    {
        public int Value { get; set; }
    }
}
