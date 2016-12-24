using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.States;

namespace Hangfire.Firebase.Entities
{
    internal class State
    {
        public string Name { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedOn { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}
