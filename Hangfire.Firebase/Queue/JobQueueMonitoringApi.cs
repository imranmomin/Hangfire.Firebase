using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FireSharp;
using FireSharp.Response;

namespace Hangfire.Firebase.Queue
{
    internal class JobQueueMonitoringApi : IPersistentJobQueueMonitoringApi
    {
        private readonly FirebaseConnection connection;
        private readonly IEnumerable<string> queues;

        public JobQueueMonitoringApi(FirebaseStorage storage)
        {
            connection = (FirebaseConnection)storage.GetConnection();
            queues = storage.Options.Queues;
        }

        public IEnumerable<string> GetQueues() => queues;

        public int GetEnqueuedCount(string queue)
        {
            FirebaseResponse response = connection.Client.Get($"queue/{queue}");
            if (response.StatusCode == HttpStatusCode.OK && !response.IsNull())
            {
                Dictionary<string, string> collection = response.ResultAs<Dictionary<string, string>>();
                return collection.Count();
            }
            return default(int);
        }

        public IEnumerable<string> GetEnqueuedJobIds(string queue, int from, int perPage)
        {
            FirebaseResponse response = connection.Client.Get($"queue/{queue}");
            if (response.StatusCode == HttpStatusCode.OK && !response.IsNull())
            {
                Dictionary<string, string> collection = response.ResultAs<Dictionary<string, string>>();
                return collection.Skip(from).Take(perPage).Select(c => c.Value);
            }
            return null;
        }

        public IEnumerable<string> GetFetchedJobIds(string queue, int from, int perPage) => GetEnqueuedJobIds(queue, from, perPage);

    }
}