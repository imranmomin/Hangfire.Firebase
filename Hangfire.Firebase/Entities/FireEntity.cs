using System;
using Newtonsoft.Json;

namespace Hangfire.Firebase.Entities
{
    internal class FireEntity
    {
        [JsonProperty(".priority")]
        private long Priority { get; set; } = DateTime.UtcNow.Ticks;
    }
}
