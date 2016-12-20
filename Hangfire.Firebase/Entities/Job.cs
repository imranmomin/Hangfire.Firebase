using System;
using Hangfire.Storage;

namespace Hangfire.Firebase.Entities
{
    internal class Job
    {
        public InvocationData InvocationData { get; set; }
        public string Arguments { get; set; }
        public string StateName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ExpireOn { get; set; }
    }
}
