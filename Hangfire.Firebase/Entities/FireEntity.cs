using System;
using Newtonsoft.Json;

namespace Hangfire.Firebase.Entities
{
    internal class FireEntity
    {
        [JsonProperty(".prority")]
        private long Prority { get; set; } = DateTime.UtcNow.Ticks;
    }
}
