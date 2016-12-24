using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Firebase.Entities
{
    internal class Counter
    {
        public int Value { get; set; }
        public DateTime? ExpireOn { get; set; }
    }
}
