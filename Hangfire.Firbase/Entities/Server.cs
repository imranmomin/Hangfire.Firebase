using System;

namespace Hangfire.Firbase.Entities
{
    internal class Server
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }
}