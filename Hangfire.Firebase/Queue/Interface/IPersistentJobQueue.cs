using Hangfire.Storage;
using System.Threading;

namespace Hangfire.Firebase.Queue
{
    internal interface IPersistentJobQueue
    {
        IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken);
        void Enqueue(string queue, string jobId);
    }
}
