using System;

namespace Hangfire.Firebase.Entities
{
    internal class List
    {
        public string Value { get; set; }
        public DateTime? ExpireOn { get; set; }
    }
}
