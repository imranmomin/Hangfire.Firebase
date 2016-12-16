using System.Threading;
using System.Threading.Tasks;
using Hangfire.Storage;
using FireSharp.Response;

namespace Hangfire.Firbase.Queue
{
    internal class JobQueue : IPersistentJobQueue
    {
        private readonly FirebaseStorage storage;
        private readonly FirebaseConnection connection;
        private EventStreamResponse response;

        public JobQueue(FirebaseStorage storage)
        {
            this.storage = storage;
            this.connection = (FirebaseConnection)storage.GetConnection();
        }

        public async Task<IFetchedJob> Dequeue(string[] queues, CancellationToken cancellationToken)
        {
            FetchedJob job = null;

            response = await connection.Client.OnAsync("queue", (sender, args, context) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                job = new FetchedJob(storage, string.Empty, args.Data);
                response.Cancel();
            });
            response.Dispose();

            return job;
        }

        public void Enqueue(string queue, string jobId) => connection.Client.Push($"queue/{queue}", jobId);
    }
}
