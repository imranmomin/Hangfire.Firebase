using System;

namespace Hangfire.Firebase.Entities
{
    internal class List : FireEntity, IExpireEntity
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
