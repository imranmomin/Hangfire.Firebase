using Hangfire.Storage;
using System.Threading;

namespace Hangfire.Firebase.Queue
{
    public interface IPersistentJobQueue
    {
        IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken);
        void Enqueue(string queue, string jobId);
    }
}
