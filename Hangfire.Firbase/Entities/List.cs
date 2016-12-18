using System;

namespace Hangfire.Firbase.Entities
{
    internal class List
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime? ExpireOn { get; set; }
    }
}
