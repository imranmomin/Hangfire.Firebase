using System;
using Newtonsoft.Json;

namespace Hangfire.Firebase.Entities
{
    internal class FireEntity : IExpireEntity
    {
        [JsonProperty(".priority")]
        private long Priority { get; set; } = DateTime.UtcNow.Ticks;
        public DateTime? ExpireOn { get; set; }
    }
}
