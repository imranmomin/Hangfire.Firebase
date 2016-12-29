using System;

namespace Hangfire.Firebase.Entities
{
    internal class List : FireEntity, IExpireEntity
    {
        public string Value { get; set; }
    }
}
