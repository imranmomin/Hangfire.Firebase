using System.Net;
using FireSharp.Response;
using Hangfire.Storage;

namespace Hangfire.Firbase.Queue
{
    internal class FetchedJob : IFetchedJob
    {
        private FirebaseConnection connection;

        public FetchedJob(FirebaseStorage storage, string queue, string jobId)
        {
            this.connection = (FirebaseConnection)storage.GetConnection();
            JobId = jobId;
            Queue = queue;
        }

        public string JobId { get; }

        public string Queue { get; }

        public void Dispose()
        {
            connection = null;
        }

        public void RemoveFromQueue() => connection.Client.Delete($"queue/${Queue}/${JobId}");

        public void Requeue()
        {
            FirebaseResponse response = connection.Client.Get($"queue/${Queue}/${JobId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                connection.Client.Push($"queue/${Queue}", JobId);
            }
        }
    }
}
