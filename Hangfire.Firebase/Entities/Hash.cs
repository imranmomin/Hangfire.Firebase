using System;

namespace Hangfire.Firebase.Entities
{
    internal class Hash
    {
        public  string Field { get; set; }
        public string Value { get; set; }
        public DateTime? ExpireOn { get; set; }
    }
}
