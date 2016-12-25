using System;
using System.Collections.Generic;

namespace Hangfire.Firebase.Entities
{
    internal class State : FireEntity
    {
        public string Name { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedOn { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}