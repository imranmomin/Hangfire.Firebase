using System;

namespace Hangfire.Firebase.Entities
{
    internal class Server
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }
}