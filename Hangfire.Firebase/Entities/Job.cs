using System;
using Hangfire.Storage;

namespace Hangfire.Firebase.Entities
{
    internal class Job : FireEntity, IExpireEntity
    {
        public InvocationData InvocationData { get; set; }
        public string Arguments { get; set; }
        public string StateId { get; set; }
        public string StateName { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
