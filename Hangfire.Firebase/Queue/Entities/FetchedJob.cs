using System.Net;

using Hangfire.Storage;
using FireSharp.Response;

namespace Hangfire.Firebase.Queue
{
    internal class FetchedJob : IFetchedJob
    {
        private FirebaseConnection connection;

        public FetchedJob(FirebaseStorage storage, string queue, string jobId, string reference)
        {
            this.connection = (FirebaseConnection)storage.GetConnection();
            JobId = jobId;
            Queue = queue;
            Reference = reference;
        }

        private string Reference { get; set; }

        public string JobId { get; }

        public string Queue { get; }

        public void Dispose()
        {
        }

        public void RemoveFromQueue() => connection.Client.Delete($"queue/{Queue}/{Reference}");

        public void Requeue()
        {
            FirebaseResponse response = connection.Client.Get($"queue/{Queue}/{Reference}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                connection.Client.Push($"queue/{Queue}", JobId);
            }
        }
    }
}
