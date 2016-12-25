using System;

namespace Hangfire.Firebase.Entities
{
    internal class List : FireEntity
    {
        public string Value { get; set; }
        public DateTime? ExpireOn { get; set; }
    }
}
