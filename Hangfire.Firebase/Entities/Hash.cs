using System;

namespace Hangfire.Firebase.Entities
{
    internal class Hash
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Field { get; set; }
        public string Value { get; set; }
        public DateTime? ExpireOn { get; set; }
    }
}
