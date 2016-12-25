using System;
using Newtonsoft.Json;

namespace Hangfire.Firebase.Entities
{
    internal class Server : FireEntity
    {
        [JsonProperty("_id")]
        public string Id { get; set; }
        public int Workers { get; set; }
        public string[] Queues { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }
}